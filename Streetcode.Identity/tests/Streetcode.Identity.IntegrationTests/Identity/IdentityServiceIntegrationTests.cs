using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

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

        var outboxMessagesCount = await dbContext.OutboxMessages.CountAsync(
            message => message.Key == firstResult.Value.ToString("D"));

        Assert.Equal(1, usersCount);
        Assert.Equal(1, outboxMessagesCount);
    }
}
