using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Domain.RefreshTokens;
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

    private readonly string _connectionString;
    private readonly ServiceProvider _serviceProvider;
    private readonly FixedTimeProvider _timeProvider;

    public RefreshTokenServiceIntegrationTests(MsSqlContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _timeProvider = new FixedTimeProvider(UtcNow);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RefreshTokenOptions.SectionName}:Lifetime"] =
                    "30.00:00:00",
                [$"{RefreshTokenOptions.SectionName}:RotationGracePeriod"] =
                    "00:00:30",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(_timeProvider);
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
    public async Task RotateAsync_WhenRotatedTokenIsRetriedWithinGracePeriod_ShouldKeepReplacementActive()
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

        var oldToken = familyTokens.Single(token =>
            token.TokenHash == hasher.ComputeHash(issued.Value.Token));
        var replacement = familyTokens.Single(token =>
            token.TokenHash == hasher.ComputeHash(rotated.Value.Token));

        Assert.NotNull(oldToken.RevokedAt);
        Assert.Null(replacement.RevokedAt);
        Assert.True(replacement.IsActiveAt(_timeProvider.GetUtcNow()));
    }

    [Fact]
    public async Task RotateAsync_WhenRotatedTokenIsReusedAfterGracePeriod_ShouldRevokeItsFamily()
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

        _timeProvider.Advance(TimeSpan.FromSeconds(31));

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

    [Fact]
    public async Task RotateAsync_WhenConcurrentRotationWins_ShouldNotRevokeWinnerReplacement()
    {
        await using var setupScope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(setupScope.ServiceProvider);
        var setupService = setupScope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();
        var issued = await setupService.IssueAsync(
            user.Id,
            CancellationToken.None);

        var interceptor = new BeforeFirstSaveChangesInterceptor(
            async cancellationToken =>
            {
                await using var winnerContext = CreateDbContext();
                var winnerService = CreateRefreshTokenService(
                    winnerContext,
                    setupScope.ServiceProvider);

                var winnerResult = await winnerService.RotateAsync(
                    issued.Value.Token,
                    cancellationToken);

                Assert.True(winnerResult.IsSuccess);
            });

        await using var loserContext = CreateDbContext(interceptor);
        var loserService = CreateRefreshTokenService(
            loserContext,
            setupScope.ServiceProvider);

        var loserResult = await loserService.RotateAsync(
            issued.Value.Token,
            CancellationToken.None);

        Assert.True(loserResult.IsFailed);
        Assert.Contains(loserResult.Errors, error =>
            error.Metadata.TryGetValue("Code", out var code) &&
            Equals(code, "RefreshToken.Invalid"));

        await using var verificationContext = CreateDbContext();
        var familyTokens = await verificationContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, familyTokens.Count);
        Assert.Single(familyTokens, token => token.RevokedAt is null);
    }

    [Fact]
    public async Task RevokeFamilyAsync_WhenFirstSaveHasConcurrencyConflict_ShouldReloadAndRetry()
    {
        await using var setupScope = _serviceProvider.CreateAsyncScope();

        var user = await CreateUserAsync(setupScope.ServiceProvider);
        var hasher = setupScope.ServiceProvider
            .GetRequiredService<IRefreshTokenHasher>();
        var familyId = Guid.NewGuid();
        const string firstRawToken = "first-refresh-token";
        const string secondRawToken = "second-refresh-token";
        var firstToken = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            familyId,
            hasher.ComputeHash(firstRawToken),
            UtcNow,
            UtcNow.AddDays(30));
        var secondToken = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            familyId,
            hasher.ComputeHash(secondRawToken),
            UtcNow,
            UtcNow.AddDays(30));

        var setupContext = setupScope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();
        setupContext.RefreshTokens.AddRange(firstToken, secondToken);
        await setupContext.SaveChangesAsync();

        var interceptor = new BeforeFirstSaveChangesInterceptor(
            async cancellationToken =>
            {
                await using var concurrentContext = CreateDbContext();
                var concurrentlyRevokedToken = await concurrentContext
                    .RefreshTokens
                    .SingleAsync(
                        token => token.Id == firstToken.Id,
                        cancellationToken);

                concurrentlyRevokedToken.Revoke(UtcNow);
                await concurrentContext.SaveChangesAsync(cancellationToken);
            });

        await using var retryingContext = CreateDbContext(interceptor);
        var service = CreateRefreshTokenService(
            retryingContext,
            setupScope.ServiceProvider);

        var result = await service.RevokeFamilyAsync(
            firstRawToken,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verificationContext = CreateDbContext();
        var familyTokens = await verificationContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.FamilyId == familyId)
            .ToListAsync();

        Assert.Equal(2, familyTokens.Count);
        Assert.All(familyTokens, token => Assert.Equal(UtcNow, token.RevokedAt));
    }

    private StreetcodeIdentityDbContext CreateDbContext(
        SaveChangesInterceptor? interceptor = null)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
                .UseSqlServer(_connectionString);

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new StreetcodeIdentityDbContext(optionsBuilder.Options);
    }

    private static RefreshTokenService CreateRefreshTokenService(
        StreetcodeIdentityDbContext dbContext,
        IServiceProvider serviceProvider)
    {
        return new RefreshTokenService(
            dbContext,
            serviceProvider.GetRequiredService<IRefreshTokenGenerator>(),
            serviceProvider.GetRequiredService<IRefreshTokenHasher>(),
            serviceProvider.GetRequiredService<IOptions<RefreshTokenOptions>>(),
            serviceProvider.GetRequiredService<TimeProvider>());
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
        private DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }

    private sealed class BeforeFirstSaveChangesInterceptor
        : SaveChangesInterceptor
    {
        private readonly Func<CancellationToken, Task> _beforeSaveChanges;
        private int _hasRun;

        public BeforeFirstSaveChangesInterceptor(
            Func<CancellationToken, Task> beforeSaveChanges)
        {
            _beforeSaveChanges = beforeSaveChanges;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _hasRun, 1) == 0)
            {
                await _beforeSaveChanges(cancellationToken);
            }

            return result;
        }
    }
}
