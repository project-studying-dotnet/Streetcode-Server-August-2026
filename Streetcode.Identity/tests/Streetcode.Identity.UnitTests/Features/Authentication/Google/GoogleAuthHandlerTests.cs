using FluentResults;
using Moq;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Google;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Google;

public sealed class GoogleAuthHandlerTests
{
    [Fact]
    public async Task Login_InvalidGoogleToken_DoesNotAccessAccountsOrIssueTokens()
    {
        var verifier = new Mock<IGoogleTokenVerifier>();
        verifier.Setup(x => x.VerifyAsync("invalid", CancellationToken.None))
            .ReturnsAsync(Result.Fail<GoogleAccount>(GoogleAuthErrors.InvalidToken()));
        var identity = new Mock<IGoogleIdentityService>(MockBehavior.Strict);
        var refresh = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var handler = new GoogleLoginCommandHandler(verifier.Object, identity.Object, refresh.Object, jwt.Object);

        var result = await handler.Handle(new GoogleLoginCommand("invalid"), CancellationToken.None);

        Assert.Equal("Google.InvalidToken", result.Errors.Single().Metadata["Code"]);
    }

    [Fact]
    public async Task Login_EmailCollision_DoesNotIssueSession()
    {
        var account = new GoogleAccount("subject", "user@example.com");
        using var cancellation = new CancellationTokenSource();
        var verifier = new Mock<IGoogleTokenVerifier>();
        verifier.Setup(x => x.VerifyAsync("token", cancellation.Token)).ReturnsAsync(Result.Ok(account));
        var identity = new Mock<IGoogleIdentityService>();
        identity.Setup(x => x.AuthenticateAsync(account, cancellation.Token))
            .ReturnsAsync(Result.Fail<UserTokenData>(GoogleAuthErrors.LinkRequired()));
        var handler = new GoogleLoginCommandHandler(verifier.Object, identity.Object,
            new Mock<IRefreshTokenService>(MockBehavior.Strict).Object, new Mock<IJwtService>(MockBehavior.Strict).Object);

        var result = await handler.Handle(new GoogleLoginCommand("token"), cancellation.Token);

        Assert.Equal("Google.LinkRequired", result.Errors.Single().Metadata["Code"]);
        identity.VerifyAll();
    }

    [Fact]
    public async Task Login_ReturningGoogleUser_IssuesStreetcodeTokens()
    {
        var account = new GoogleAccount("subject", "user@example.com");
        var user = new UserTokenData(Guid.NewGuid(), account.Email, ["User"], 3, true);
        var expires = DateTime.UtcNow.AddMinutes(10);
        var verifier = new Mock<IGoogleTokenVerifier>();
        verifier.Setup(x => x.VerifyAsync("token", CancellationToken.None)).ReturnsAsync(Result.Ok(account));
        var identity = new Mock<IGoogleIdentityService>();
        identity.Setup(x => x.AuthenticateAsync(account, CancellationToken.None)).ReturnsAsync(Result.Ok(user));
        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(x => x.IssueAsync(user.UserId, CancellationToken.None)).ReturnsAsync(
            Result.Ok(new RefreshTokenResult(user.UserId, "refresh", expires.AddDays(1))));
        var jwt = new Mock<IJwtService>();
        jwt.Setup(x => x.GenerateToken(user.UserId, user.Email, user.Roles, user.AccessVersion))
            .Returns(new AuthTokenResult("access", expires));

        var result = await new GoogleLoginCommandHandler(verifier.Object, identity.Object, refresh.Object, jwt.Object)
            .Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal("refresh", result.Value.RefreshToken);
        jwt.VerifyAll();
    }

    [Fact]
    public void Link_RequiresExplicitConfirmationAndPassword()
    {
        var validator = new LinkGoogleCommandValidator();
        var result = validator.Validate(new LinkGoogleCommand("token", Guid.NewGuid(), 1, "", false));
        Assert.Contains(result.Errors, error => error.PropertyName == "ConfirmLink");
        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }

    [Fact]
    public async Task Link_ForwardsVerifiedIdentityAndCaller()
    {
        var account = new GoogleAccount("subject", "user@example.com");
        var command = new LinkGoogleCommand("token", Guid.NewGuid(), 2, "Password123!", true);
        using var cancellation = new CancellationTokenSource();
        var verifier = new Mock<IGoogleTokenVerifier>();
        verifier.Setup(x => x.VerifyAsync(command.IdToken, cancellation.Token)).ReturnsAsync(Result.Ok(account));
        var identity = new Mock<IGoogleIdentityService>();
        identity.Setup(x => x.LinkAsync(account, command.UserId, 2, command.Password, cancellation.Token))
            .ReturnsAsync(Result.Ok());

        var result = await new LinkGoogleCommandHandler(verifier.Object, identity.Object).Handle(command, cancellation.Token);

        Assert.True(result.IsSuccess);
        identity.VerifyAll();
    }
}
