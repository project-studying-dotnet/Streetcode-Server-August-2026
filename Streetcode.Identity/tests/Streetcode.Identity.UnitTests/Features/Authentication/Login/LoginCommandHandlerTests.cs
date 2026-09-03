using FluentResults;
using Moq;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Login;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Login;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _identityService.Object,
            _jwtService.Object,
            _refreshTokenService.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthenticationFails_ShouldReturnFailureAndStopProcessing()
    {
        const string email = "user@example.com";
        const string password = "WrongPassword";
        var authenticationError =
            new Error("Invalid email or password")
                .WithMetadata("Code", "Identity.InvalidCredentials");

        _identityService
            .Setup(service => service.AuthenticateAsync(
                email,
                password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Fail<UserTokenData>(authenticationError));

        var result = await _handler.Handle(
            new LoginCommand(email, password),
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.Equal(authenticationError.Message, error.Message);
        Assert.Equal(
            "Identity.InvalidCredentials",
            error.Metadata["Code"]);

        VerifyJwtWasNotGenerated();

        _refreshTokenService.Verify(
            service => service.IssueAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIssuingFails_ShouldReturnFailureAndNotGenerateJwt()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword123!";
        var userId = Guid.NewGuid();
        var roles = new[] { "TestRole" };
        var userData = new UserTokenData(
            userId,
            email,
            roles,
            AccessVersion: 3,
            IsActive: true);
        var refreshError = new Error("Refresh token issuing failed")
            .WithMetadata("Code", "RefreshToken.InvalidUser");

        _identityService
            .Setup(service => service.AuthenticateAsync(
                email,
                password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(userData));

        _refreshTokenService
            .Setup(service => service.IssueAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Fail<RefreshTokenResult>(refreshError));

        var result = await _handler.Handle(
            new LoginCommand(email, password),
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.Equal(refreshError.Message, error.Message);
        Assert.Equal(
            "RefreshToken.InvalidUser",
            error.Metadata["Code"]);

        VerifyJwtWasNotGenerated();

        _refreshTokenService.Verify(
            service => service.IssueAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnAccessAndRefreshTokens()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword123!";
        const string accessTokenValue = "generated-access-token";
        const string refreshTokenValue = "generated-refresh-token";
        const long accessVersion = 5;
        var userId = Guid.NewGuid();
        var roles = new[] { "TestRoleA", "TestRoleB" };
        var accessExpiresAt = DateTime.SpecifyKind(
            DateTime.UtcNow.AddMinutes(60),
            DateTimeKind.Utc);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var userData = new UserTokenData(
            userId,
            email,
            roles,
            accessVersion,
            IsActive: true);

        _identityService
            .Setup(service => service.AuthenticateAsync(
                email,
                password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(userData));

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

        _refreshTokenService
            .Setup(service => service.IssueAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new RefreshTokenResult(
                userId,
                refreshTokenValue,
                refreshExpiresAt)));

        var result = await _handler.Handle(
            new LoginCommand(email, password),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(accessTokenValue, result.Value.AccessToken);
        Assert.Equal(
            new DateTimeOffset(accessExpiresAt),
            result.Value.AccessTokenExpiresAt);
        Assert.Equal(refreshTokenValue, result.Value.RefreshToken);
        Assert.Equal(
            refreshExpiresAt,
            result.Value.RefreshTokenExpiresAt);

        _identityService.Verify(
            service => service.AuthenticateAsync(
                email,
                password,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _jwtService.VerifyAll();

        _refreshTokenService.Verify(
            service => service.IssueAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
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
}
