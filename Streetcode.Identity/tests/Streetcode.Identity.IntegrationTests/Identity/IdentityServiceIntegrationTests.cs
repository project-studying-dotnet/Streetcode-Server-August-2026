using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Identity;

public sealed class IdentityServiceIntegrationTests
    : IClassFixture<MsSqlContainerFixture>, IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public IdentityServiceIntegrationTests(MsSqlContainerFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(fixture.ConnectionString);

        _serviceProvider =
            services.BuildServiceProvider(validateScopes: true);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task CreateUserAsync_WhenCredentialsAreValid_ShouldPersistHashedUser()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "ValidPassword123!";

        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var creationResult = await identityService.CreateUserAsync(
            email,
            password,
            CancellationToken.None);

        var user = await userManager.FindByEmailAsync(email);

        Assert.True(creationResult.IsSuccess);
        Assert.NotNull(user);

        Assert.Equal(creationResult.Value, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(email, user.UserName);

        Assert.Equal(userManager.NormalizeEmail(email), user.NormalizedEmail);

        Assert.Equal(userManager.NormalizeName(email), user.NormalizedUserName);

        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual(password, user.PasswordHash);

        var passwordIsValid =
            await userManager.CheckPasswordAsync(user, password);

        Assert.True(passwordIsValid);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ShouldFailAndKeepSingleUser()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "ValidPassword123!";

        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var firstResult = await identityService.CreateUserAsync(
            email,
            password,
            CancellationToken.None);

        var secondResult = await identityService.CreateUserAsync(
            email,
            password,
            CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsFailed);

        var hasDuplicateEmailError = secondResult.Errors.Any(error =>
            error.Metadata.TryGetValue("Code", out var code) &&
            Equals(code, "DuplicateEmail"));

        Assert.True(hasDuplicateEmailError);

        var normalizedEmail = userManager.NormalizeEmail(email);

        var usersCount = await userManager.Users.CountAsync(
            user => user.NormalizedEmail == normalizedEmail);

        Assert.Equal(1, usersCount);
    }
}
