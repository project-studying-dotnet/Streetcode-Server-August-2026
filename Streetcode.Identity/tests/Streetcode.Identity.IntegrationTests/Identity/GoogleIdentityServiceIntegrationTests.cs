using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Identity;

[Collection(MsSqlCollection.Name)]
public sealed class GoogleIdentityServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private const string Password = "ValidPassword123!";

    public GoogleIdentityServiceIntegrationTests(MsSqlContainerFixture fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddInfrastructure(fixture.ConnectionString);
        _provider = services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task FirstGoogleLogin_CreatesPasswordlessUserRoleLoginAndOutbox_ThenReusesSameUser()
    {
        await using var scope = _provider.CreateAsyncScope();
        await EnsureRole(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IGoogleIdentityService>();
        var account = Account();
        var first = await service.AuthenticateAsync(account, CancellationToken.None);
        var second = await service.AuthenticateAsync(account, CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value.UserId, second.Value.UserId);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(first.Value.UserId.ToString());
        Assert.NotNull(user);
        Assert.False(await users.HasPasswordAsync(user));
        Assert.True(await users.IsInRoleAsync(user, "User"));
        Assert.Single(await users.GetLoginsAsync(user));
        var db = scope.ServiceProvider.GetRequiredService<StreetcodeIdentityDbContext>();
        Assert.Equal(1, await db.OutboxMessages.CountAsync(x => x.Key == user.Id.ToString("D")));
    }

    [Fact]
    public async Task MatchingEmail_DoesNotLinkUntilExplicitAuthenticatedLink_AndPreservesPassword()
    {
        await using var scope = _provider.CreateAsyncScope();
        await EnsureRole(scope.ServiceProvider);
        var account = Account();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await LocalUser(users, account.Email);
        var originalHash = user.PasswordHash;
        var service = scope.ServiceProvider.GetRequiredService<IGoogleIdentityService>();

        var collision = await service.AuthenticateAsync(account, CancellationToken.None);
        Assert.Equal("Google.LinkRequired", collision.Errors.Single().Metadata["Code"]);
        Assert.Empty(await users.GetLoginsAsync(user));

        var linked = await service.LinkAsync(account, user.Id, user.AccessVersion, Password, CancellationToken.None);
        Assert.True(linked.IsSuccess);
        var login = await service.AuthenticateAsync(account, CancellationToken.None);
        Assert.True(login.IsSuccess);
        Assert.Equal(user.Id, login.Value.UserId);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, Password));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("version")]
    [InlineData("inactive")]
    public async Task Link_InvalidProofOrAccount_DoesNotCreateLogin(string scenario)
    {
        await using var scope = _provider.CreateAsyncScope();
        var account = Account();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await LocalUser(users, account.Email);
        if (scenario == "inactive") { user.Deactivate(); await users.UpdateAsync(user); }
        var service = scope.ServiceProvider.GetRequiredService<IGoogleIdentityService>();
        var result = await service.LinkAsync(
            scenario == "email" ? account with { Email = "other@example.com" } : account,
            user.Id, scenario == "version" ? user.AccessVersion + 1 : user.AccessVersion,
            scenario == "password" ? "WrongPassword123!" : Password, CancellationToken.None);
        Assert.True(result.IsFailed);
        Assert.Empty(await users.GetLoginsAsync(user));
    }

    [Fact]
    public async Task Link_GoogleSubjectOwnedByAnotherUser_CannotReassign()
    {
        await using var scope = _provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var account = Account();
        var owner = await LocalUser(users, $"owner-{Guid.NewGuid():N}@example.com");
        Assert.True((await users.AddLoginAsync(owner, new UserLoginInfo("Google", account.Subject, "Google"))).Succeeded);
        var target = await LocalUser(users, account.Email);
        var result = await scope.ServiceProvider.GetRequiredService<IGoogleIdentityService>()
            .LinkAsync(account, target.Id, target.AccessVersion, Password, CancellationToken.None);
        Assert.True(result.IsFailed);
        Assert.Empty(await users.GetLoginsAsync(target));
        Assert.Equal(owner.Id, (await users.FindByLoginAsync("Google", account.Subject))!.Id);
    }

    [Fact]
    public async Task ReturningGoogleUser_WhenDisabled_CannotLogin()
    {
        await using var scope = _provider.CreateAsyncScope();
        await EnsureRole(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IGoogleIdentityService>();
        var account = Account();
        var first = await service.AuthenticateAsync(account, CancellationToken.None);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(first.Value.UserId.ToString()))!;
        user.Deactivate();
        await users.UpdateAsync(user);
        Assert.True((await service.AuthenticateAsync(account, CancellationToken.None)).IsFailed);
    }

    private static GoogleAccount Account() => new(Guid.NewGuid().ToString("N"), $"google-{Guid.NewGuid():N}@example.com");

    private static async Task<ApplicationUser> LocalUser(UserManager<ApplicationUser> users, string email)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        return user;
    }

    private static async Task EnsureRole(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roles.RoleExistsAsync("User"))
            Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("User"))).Succeeded);
    }

    public void Dispose() => _provider.Dispose();
}
