using FluentResults;
using Moq;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Refresh;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Refresh;

public sealed class RefreshSessionCommandHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IJwtService> _jwtService = new();

    private readonly RefreshSessionCommandHandler _handler;

    public RefreshSessionCommandHandlerTests()
    {
        _handler = new RefreshSessionCommandHandler(
            _refreshTokenService.Object,
            _identityService.Object,
            _jwtService.Object);
    }

    [Fact]
    public async Task Handle_WhenRotationFails_ShouldReturnFailureAndStopProcessing()
    {
        const string rawToken = "invalid-refresh-token";
        var rotationError = new Error("The refresh token is invalid")
            .WithMetadata("Code", "RefreshToken.Invalid");

        _refreshTokenService
            .Setup(service => service.RotateAsync(
                rawToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<RefreshTokenResult>(rotationError));

        var result = await _handler.Handle(
            new RefreshSessionCommand(rawToken),
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var returnedError = Assert.Single(result.Errors);
        Assert.Equal(rotationError.Message, returnedError.Message);
        Assert.Equal(
            rotationError.Metadata["Code"],
            returnedError.Metadata["Code"]);

        _identityService.Verify(
            service => service.GetUserTokenDataAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _refreshTokenService.Verify(
            service => service.RevokeFamilyAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyJwtWasNotGenerated();
    }

    [Fact]
    public async Task Handle_WhenUserLookupFails_ShouldRevokeFamilyAndReturnInvalidSession()
    {
        const string oldToken = "old-refresh-token";
        const string replacementToken = "replacement-refresh-token";
        var userId = Guid.NewGuid();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(30);

        SetupSuccessfulRotation(
            oldToken,
            new RefreshTokenResult(
                userId,
                replacementToken,
                refreshExpiresAt));

        _identityService
            .Setup(service => service.GetUserTokenDataAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<UserTokenData>(
                new Error("The user could not be loaded")
                    .WithMetadata("Code", "Identity.UserNotFound")));

        _refreshTokenService
            .Setup(service => service.RevokeFamilyAsync(
                replacementToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var result = await _handler.Handle(
            new RefreshSessionCommand(oldToken),
            CancellationToken.None);

        AssertInvalidSession(result);

        _refreshTokenService.Verify(
            service => service.RevokeFamilyAsync(
                replacementToken,
                It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyJwtWasNotGenerated();
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ShouldRevokeFamilyAndReturnInvalidSession()
    {
        const string oldToken = "old-refresh-token";
        const string replacementToken = "replacement-refresh-token";
        var userId = Guid.NewGuid();

        SetupSuccessfulRotation(
            oldToken,
            new RefreshTokenResult(
                userId,
                replacementToken,
                DateTimeOffset.UtcNow.AddDays(30)));

        _identityService
            .Setup(service => service.GetUserTokenDataAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new UserTokenData(
                userId,
                "inactive@example.com",
                Array.Empty<string>(),
                AccessVersion: 2,
                IsActive: false)));

        _refreshTokenService
            .Setup(service => service.RevokeFamilyAsync(
                replacementToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var result = await _handler.Handle(
            new RefreshSessionCommand(oldToken),
            CancellationToken.None);

        AssertInvalidSession(result);

        _refreshTokenService.Verify(
            service => service.RevokeFamilyAsync(
                replacementToken,
                It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyJwtWasNotGenerated();
    }

    [Fact]
    public async Task Handle_WhenSessionIsValid_ShouldReturnRotatedRefreshAndAccessTokens()
    {
        const string oldToken = "old-refresh-token";
        const string replacementToken = "replacement-refresh-token";
        const string accessTokenValue = "generated-access-token";
        const string email = "active@example.com";
        const long accessVersion = 7;
        var roles = new[] { "TestRoleA", "TestRoleB" };
        var userId = Guid.NewGuid();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var accessExpiresAt = DateTime.SpecifyKind(
            DateTime.UtcNow.AddMinutes(60),
            DateTimeKind.Utc);

        SetupSuccessfulRotation(
            oldToken,
            new RefreshTokenResult(
                userId,
                replacementToken,
                refreshExpiresAt));

        _identityService
            .Setup(service => service.GetUserTokenDataAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new UserTokenData(
                userId,
                email,
                roles,
                accessVersion,
                IsActive: true)));

        _jwtService
            .Setup(service => service.GenerateToken(
                userId,
                email,
                It.Is<IEnumerable<string>>(
                    actualRoles => actualRoles.SequenceEqual(roles)),
                accessVersion))
            .Returns(new AuthTokenResult(
                accessTokenValue,
                accessExpiresAt));

        var result = await _handler.Handle(
            new RefreshSessionCommand(oldToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(accessTokenValue, result.Value.AccessToken);
        Assert.Equal(
            new DateTimeOffset(accessExpiresAt),
            result.Value.AccessTokenExpiresAt);
        Assert.Equal(replacementToken, result.Value.RefreshToken);
        Assert.Equal(
            refreshExpiresAt,
            result.Value.RefreshTokenExpiresAt);

        _refreshTokenService.Verify(
            service => service.RevokeFamilyAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _jwtService.VerifyAll();
    }

    private void SetupSuccessfulRotation(
        string rawToken,
        RefreshTokenResult result)
    {
        _refreshTokenService
            .Setup(service => service.RotateAsync(
                rawToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(result));
    }

    private void VerifyJwtWasNotGenerated()
    {
        _jwtService.Verify(
            service => service.GenerateToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<long>()),
            Times.Never);
    }

    private static void AssertInvalidSession(
        Result<RefreshSessionResponse> result)
    {
        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.Equal("RefreshToken.Invalid", error.Metadata["Code"]);
    }
}
