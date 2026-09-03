using System.Text.Json;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Identity;

[Collection(MsSqlCollection.Name)]
public sealed class IdentityServiceIntegrationTests
    : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TrackingPasswordHasher _passwordHasher = new();

    public IdentityServiceIntegrationTests(MsSqlContainerFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddAuthentication();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddSingleton<IPasswordHasher<ApplicationUser>>(
            _passwordHasher);

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
        const string phoneNumber = "+380501234567";

        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var creationResult = await identityService.CreateUserAsync(
            email,
            password,
            phoneNumber,
            CancellationToken.None);

        var user = await userManager.FindByEmailAsync(email);

        Assert.True(creationResult.IsSuccess);
        Assert.NotNull(user);

        Assert.Equal(creationResult.Value, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(email, user.UserName);
        Assert.Equal(phoneNumber, user.PhoneNumber);

        Assert.Equal(userManager.NormalizeEmail(email), user.NormalizedEmail);

        Assert.Equal(userManager.NormalizeName(email), user.NormalizedUserName);

        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual(password, user.PasswordHash);

        var passwordIsValid =
            await userManager.CheckPasswordAsync(user, password);

        Assert.True(passwordIsValid);

        var outboxMessage = await dbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync(message =>
                message.Key == creationResult.Value.ToString("D"));

        var integrationEvent = JsonSerializer.Deserialize<UserAccessChangedV1>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(integrationEvent);
        Assert.Equal(outboxMessage.Id, integrationEvent.EventId);
        Assert.Equal(user.Id, integrationEvent.UserId);
        Assert.True(integrationEvent.IsActive);
        Assert.Equal(1, integrationEvent.AccessVersion);
        Assert.Equal("UserAccessChangedV1", outboxMessage.Type);
        Assert.Null(outboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ShouldFailAndKeepSingleUser()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "ValidPassword123!";
        const string phoneNumber = "+380501234567";

        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var firstResult = await identityService.CreateUserAsync(
            email,
            password,
            phoneNumber,
            CancellationToken.None);

        var secondResult = await identityService.CreateUserAsync(
            email,
            password,
            phoneNumber,
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

        var outboxMessagesCount = await dbContext.OutboxMessages.CountAsync(
            message => message.Key == firstResult.Value.ToString("D"));

        Assert.Equal(1, usersCount);
        Assert.Equal(1, outboxMessagesCount);
    }

    [Fact]
    public async Task GetUserTokenDataAsync_WhenUserExists_ShouldReturnTokenData()
    {
        var userId = Guid.NewGuid();
        var email = $"token-user-{Guid.NewGuid():N}@example.com";
        var roleName = $"TokenRole-{Guid.NewGuid():N}";
        const string password = "ValidPassword123!";

        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email
        };

        var userCreationResult = await userManager.CreateAsync(user, password);
        var roleCreationResult = await roleManager.CreateAsync(
            new IdentityRole<Guid>(roleName));
        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);

        Assert.True(userCreationResult.Succeeded);
        Assert.True(roleCreationResult.Succeeded);
        Assert.True(addToRoleResult.Succeeded);

        var result = await identityService.GetUserTokenDataAsync(
            userId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Contains(roleName, result.Value.Roles);
        Assert.Equal(1, result.Value.AccessVersion);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task GetUserTokenDataAsync_WhenUserDoesNotExist_ShouldReturnNotFoundError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var result = await identityService.GetUserTokenDataAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.True(error.Metadata.TryGetValue("Code", out var code));
        Assert.Equal("Identity.UserNotFound", code);
    }

    [Fact]
    public async Task GetUserTokenDataAsync_WhenUserIdIsEmpty_ShouldReturnNotFoundError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var result = await identityService.GetUserTokenDataAsync(
            Guid.Empty,
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.True(error.Metadata.TryGetValue("Code", out var code));
        Assert.Equal("Identity.UserNotFound", code);
    }

    [Fact]
    public async Task GetUserTokenDataAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            identityService.GetUserTokenDataAsync(
                Guid.NewGuid(),
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialsAreValid_ShouldReturnUserTokenData()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var userId = Guid.NewGuid();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var roleName = $"LoginRole-{Guid.NewGuid():N}";
        const string password = "ValidPassword123!";

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email
        };

        var userCreationResult =
            await userManager.CreateAsync(user, password);

        var roleCreationResult =
            await roleManager.CreateAsync(
                new IdentityRole<Guid>(roleName));

        var addToRoleResult =
            await userManager.AddToRoleAsync(user, roleName);

        Assert.True(userCreationResult.Succeeded);
        Assert.True(roleCreationResult.Succeeded);
        Assert.True(addToRoleResult.Succeeded);

        var result = await identityService.AuthenticateAsync(
            email.ToUpperInvariant(),
            password,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Contains(roleName, result.Value.Roles);
        Assert.Equal(1, result.Value.AccessVersion);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordIsInvalid_ShouldReturnGenericErrorAndRecordFailure()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"wrong-password-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var userCreationResult =
            await userManager.CreateAsync(user, "ValidPassword123!");

        Assert.True(userCreationResult.Succeeded);

        var result = await identityService.AuthenticateAsync(
            email,
            "WrongPassword123!",
            CancellationToken.None);

        AssertInvalidCredentials(result);
        Assert.Equal(1, await userManager.GetAccessFailedCountAsync(user));
        Assert.Equal(1, _passwordHasher.VerificationCount);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEmailDoesNotExist_ShouldRunDummyVerificationAndReturnGenericError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var result = await identityService.AuthenticateAsync(
            $"missing-{Guid.NewGuid():N}@example.com",
            "ValidPassword123!",
            CancellationToken.None);

        AssertInvalidCredentials(result);
        Assert.Equal(1, _passwordHasher.VerificationCount);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserIsInactive_ShouldReturnGenericError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"inactive-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var userCreationResult =
            await userManager.CreateAsync(user, "ValidPassword123!");

        Assert.True(userCreationResult.Succeeded);
        Assert.True(user.Deactivate());

        var updateResult = await userManager.UpdateAsync(user);

        Assert.True(updateResult.Succeeded);

        var result = await identityService.AuthenticateAsync(
            email,
            "ValidPassword123!",
            CancellationToken.None);

        AssertInvalidCredentials(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenMaximumFailedAttemptsReached_ShouldLockUserOut()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var identityOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<IdentityOptions>>()
            .Value;

        var email = $"lockout-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var userCreationResult =
            await userManager.CreateAsync(user, "ValidPassword123!");

        Assert.True(userCreationResult.Succeeded);
        Assert.True(user.LockoutEnabled);
        Assert.True(identityOptions.Lockout.MaxFailedAccessAttempts > 0);

        for (var attempt = 0;
             attempt < identityOptions.Lockout.MaxFailedAccessAttempts;
             attempt++)
        {
            var failedResult = await identityService.AuthenticateAsync(
                email,
                "WrongPassword123!",
                CancellationToken.None);

            AssertInvalidCredentials(failedResult);
        }

        Assert.True(await userManager.IsLockedOutAsync(user));

        var resultWithCorrectPassword =
            await identityService.AuthenticateAsync(
                email,
                "ValidPassword123!",
                CancellationToken.None);

        AssertInvalidCredentials(resultWithCorrectPassword);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var identityService =
            scope.ServiceProvider.GetRequiredService<IIdentityService>();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            identityService.AuthenticateAsync(
                "user@example.com",
                "ValidPassword123!",
                cancellationTokenSource.Token));
    }

    private static void AssertInvalidCredentials(
        Result<UserTokenData> result)
    {
        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Invalid email or password", error.Message);
        Assert.True(error.Metadata.TryGetValue("Code", out var code));
        Assert.Equal("Identity.InvalidCredentials", code);
    }

    private sealed class TrackingPasswordHasher
        : IPasswordHasher<ApplicationUser>
    {
        private readonly PasswordHasher<ApplicationUser> _inner = new();

        public int VerificationCount { get; private set; }

        public string HashPassword(
            ApplicationUser user,
            string password)
        {
            return _inner.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(
            ApplicationUser user,
            string hashedPassword,
            string providedPassword)
        {
            VerificationCount++;

            return _inner.VerifyHashedPassword(
                user,
                hashedPassword,
                providedPassword);
        }
    }
}
