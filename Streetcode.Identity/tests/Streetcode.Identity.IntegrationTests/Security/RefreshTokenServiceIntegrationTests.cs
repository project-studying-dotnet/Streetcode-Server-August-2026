using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Security;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Security;

[Collection(MsSqlCollection.Name)]
public sealed class RefreshTokenServiceIntegrationTests : IDisposable
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _serviceProvider;

    public RefreshTokenServiceIntegrationTests(MsSqlContainerFixture fixture)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RefreshTokenOptions.SectionName}:Lifetime"] =
                    "30.00:00:00",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow));
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddRefreshTokenServices(configuration);

        _serviceProvider =
            services.BuildServiceProvider(validateScopes: true);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task IssueAsync_WhenUserIsActive_ShouldStoreHashAndReturnRawToken()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();
        var hasher = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenHasher>();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var result = await service.IssueAsync(
            user.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.Equal(UtcNow.AddDays(30), result.Value.ExpiresAt);

        dbContext.ChangeTracker.Clear();

        var storedToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.UserId == user.Id);

        Assert.NotEqual(result.Value.Token, storedToken.TokenHash);
        Assert.Equal(
            hasher.ComputeHash(result.Value.Token),
            storedToken.TokenHash);
        Assert.Equal(UtcNow, storedToken.CreatedAt);
        Assert.Equal(result.Value.ExpiresAt, storedToken.ExpiresAt);
        Assert.True(storedToken.IsActiveAt(UtcNow));
    }

    [Fact]
    public async Task RotateAsync_WhenTokenIsActive_ShouldRevokeOldAndIssueReplacementInSameFamily()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();
        var hasher = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenHasher>();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var issued = await service.IssueAsync(
            user.Id,
            CancellationToken.None);
        var rotated = await service.RotateAsync(
            issued.Value.Token,
            CancellationToken.None);

        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(issued.Value.Token, rotated.Value.Token);
        Assert.Equal(user.Id, rotated.Value.UserId);

        dbContext.ChangeTracker.Clear();

        var tokens = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == user.Id)
            .OrderBy(token => token.CreatedAt)
            .ThenBy(token => token.Id)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);

        var oldToken = tokens.Single(token =>
            token.TokenHash == hasher.ComputeHash(issued.Value.Token));
        var replacement = tokens.Single(token =>
            token.TokenHash == hasher.ComputeHash(rotated.Value.Token));

        Assert.Equal(oldToken.FamilyId, replacement.FamilyId);
        Assert.Equal(replacement.Id, oldToken.ReplacedByTokenId);
        Assert.Equal(UtcNow, oldToken.RevokedAt);
        Assert.Null(replacement.RevokedAt);
        Assert.True(replacement.IsActiveAt(UtcNow));
    }

    [Fact]
    public async Task RotateAsync_WhenRotatedTokenIsReused_ShouldRevokeItsFamily()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var issued = await service.IssueAsync(
            user.Id,
            CancellationToken.None);
        var rotated = await service.RotateAsync(
            issued.Value.Token,
            CancellationToken.None);
        var reused = await service.RotateAsync(
            issued.Value.Token,
            CancellationToken.None);

        Assert.True(rotated.IsSuccess);
        Assert.True(reused.IsFailed);
        Assert.Contains(reused.Errors, error =>
            error.Metadata.TryGetValue("Code", out var code) &&
            Equals(code, "RefreshToken.Invalid"));

        dbContext.ChangeTracker.Clear();

        var familyTokens = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, familyTokens.Count);
        Assert.All(familyTokens, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task RevokeFamilyAsync_WhenCalledTwice_ShouldRemainSuccessfulAndRevokeAllTokens()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var issued = await service.IssueAsync(
            user.Id,
            CancellationToken.None);
        var rotated = await service.RotateAsync(
            issued.Value.Token,
            CancellationToken.None);

        var firstRevocation = await service.RevokeFamilyAsync(
            rotated.Value.Token,
            CancellationToken.None);
        var secondRevocation = await service.RevokeFamilyAsync(
            rotated.Value.Token,
            CancellationToken.None);

        Assert.True(firstRevocation.IsSuccess);
        Assert.True(secondRevocation.IsSuccess);

        dbContext.ChangeTracker.Clear();

        var familyTokens = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, familyTokens.Count);
        Assert.All(familyTokens, token => Assert.NotNull(token.RevokedAt));
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        IServiceProvider serviceProvider)
    {
        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
        };

        var creationResult = await userManager.CreateAsync(
            user,
            "ValidPassword123!");

        Assert.True(
            creationResult.Succeeded,
            string.Join(
                Environment.NewLine,
                creationResult.Errors.Select(error => error.Description)));

        return user;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
