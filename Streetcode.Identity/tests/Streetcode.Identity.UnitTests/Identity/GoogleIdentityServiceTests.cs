using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Infrastructure.Identity;

namespace Streetcode.Identity.UnitTests.Identity;

public sealed class GoogleIdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _users = new(
        Mock.Of<IUserStore<ApplicationUser>>(), Options.Create(new IdentityOptions()),
        Mock.Of<IPasswordHasher<ApplicationUser>>(), Array.Empty<IUserValidator<ApplicationUser>>(),
        Array.Empty<IPasswordValidator<ApplicationUser>>(), Mock.Of<ILookupNormalizer>(),
        new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), Mock.Of<ILogger<UserManager<ApplicationUser>>>());

    private Mock<SignInManager<ApplicationUser>> SignIn() => new(_users.Object,
        Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
        Options.Create(new IdentityOptions()), Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
        Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<ApplicationUser>>());

    [Fact]
    public async Task ExistingEmail_IsNeverAutomaticallyLinked()
    {
        var account = new GoogleAccount("subject", "user@example.com");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = account.Email };
        _users.Setup(x => x.FindByEmailAsync(account.Email)).ReturnsAsync(user);
        var result = await Service().AuthenticateAsync(account, CancellationToken.None);

        Assert.Equal("Google.LinkRequired", result.Errors.Single().Metadata["Code"]);
        _users.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
        _users.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkedUser_InactiveOrLocked_IsRejected(bool locked)
    {
        var account = new GoogleAccount("subject", "user@example.com");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = account.Email };
        if (!locked) user.Deactivate();
        _users.Setup(x => x.FindByLoginAsync("Google", account.Subject)).ReturnsAsync(user);
        _users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(locked);
        var result = await Service().AuthenticateAsync(account, CancellationToken.None);
        Assert.Equal("Google.Unauthorized", result.Errors.Single().Metadata["Code"]);
    }

    [Fact]
    public async Task LinkedUser_ChangedGoogleEmail_UsesSubjectAndLocalUser()
    {
        var account = new GoogleAccount("subject", "changed@example.com");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "original@example.com" };
        _users.Setup(x => x.FindByLoginAsync("Google", account.Subject)).ReturnsAsync(user);
        _users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new[] { "User" });
        var result = await Service().AuthenticateAsync(account, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(user.Email, result.Value.Email);
        _users.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Link_WrongPassword_CannotAddLogin()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        _users.Setup(x => x.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        var signIn = SignIn();
        signIn.Setup(x => x.CheckPasswordSignInAsync(user, "wrong", true)).ReturnsAsync(SignInResult.Failed);
        var result = await Service(signIn.Object).LinkAsync(new GoogleAccount("subject", user.Email),
            user.Id, user.AccessVersion, "wrong", CancellationToken.None);
        Assert.Equal("Google.Unauthorized", result.Errors.Single().Metadata["Code"]);
        _users.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
        signIn.VerifyAll();
    }

    [Fact]
    public async Task Link_StaleAccessToken_CannotAddLogin()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        user.IncrementAccessVersion();
        _users.Setup(x => x.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        var result = await Service().LinkAsync(new GoogleAccount("subject", user.Email), user.Id, 1,
            "Password123!", CancellationToken.None);
        Assert.True(result.IsFailed);
        _users.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
    }

    [Fact]
    public async Task Link_AlreadyLinkedToSameUser_IsIdempotent()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        _users.Setup(x => x.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        _users.Setup(x => x.NormalizeEmail(user.Email)).Returns(user.Email);
        _users.Setup(x => x.FindByLoginAsync("Google", "subject")).ReturnsAsync(user);
        var signIn = SignIn();
        signIn.Setup(x => x.CheckPasswordSignInAsync(user, "Password123!", true)).ReturnsAsync(SignInResult.Success);
        var result = await Service(signIn.Object).LinkAsync(new GoogleAccount("subject", user.Email),
            user.Id, 1, "Password123!", CancellationToken.None);
        Assert.True(result.IsSuccess);
        _users.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
    }

    private GoogleIdentityService Service(SignInManager<ApplicationUser>? signIn = null) => new(
        _users.Object, signIn ?? SignIn().Object, null!, Mock.Of<IOutboxWriter>(), TimeProvider.System);
}
