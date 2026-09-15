using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Domain.RefreshTokens;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Security;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Security;

[Collection(MsSqlCollection.Name)]
public sealed class RefreshTokenCleanupServiceIntegrationTests
    : IAsyncLifetime
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan RetentionPeriod =
        TimeSpan.FromDays(7);

    private readonly MsSqlContainerFixture _fixture;

    public RefreshTokenCleanupServiceIntegrationTests(
        MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        return DeleteAllRefreshTokensAsync();
    }

    public Task DisposeAsync()
    {
        return DeleteAllRefreshTokensAsync();
    }

    [Fact]
    public async Task CleanupAsync_WhenEntireFamilyIsPastRetention_ShouldDeleteFamily()
    {
        await using var context = CreateDbContext();
        var user = await CreateUserAsync(context);
        var familyId = Guid.NewGuid();
        var (original, replacement) = CreateRotatedFamily(
            user.Id,
            familyId,
            originalCreatedAt: UtcNow.AddDays(-60),
            originalExpiresAt: UtcNow.AddDays(-30),
            replacementCreatedAt: UtcNow.AddDays(-40),
            replacementExpiresAt: UtcNow.AddDays(-20));

        context.RefreshTokens.AddRange(original, replacement);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateCleanupService(context, batchSize: 10);

        var deletedCount = await service.CleanupAsync(
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var remainingCount = await context.RefreshTokens
            .CountAsync(token => token.FamilyId == familyId);

        Assert.Equal(2, deletedCount);
        Assert.Equal(0, remainingCount);
    }

    [Fact]
    public async Task CleanupAsync_WhenFamilyHasRecentToken_ShouldKeepEntireFamily()
    {
        await using var context = CreateDbContext();
        var user = await CreateUserAsync(context);
        var familyId = Guid.NewGuid();
        var (original, replacement) = CreateRotatedFamily(
            user.Id,
            familyId,
            originalCreatedAt: UtcNow.AddDays(-60),
            originalExpiresAt: UtcNow.AddDays(-20),
            replacementCreatedAt: UtcNow.AddDays(-30),
            replacementExpiresAt: UtcNow.AddDays(1));

        context.RefreshTokens.AddRange(original, replacement);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateCleanupService(context, batchSize: 10);

        var deletedCount = await service.CleanupAsync(
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var remainingCount = await context.RefreshTokens
            .CountAsync(token => token.FamilyId == familyId);

        Assert.Equal(0, deletedCount);
        Assert.Equal(2, remainingCount);
    }

    [Fact]
    public async Task CleanupAsync_WhenMoreFamiliesAreEligibleThanBatchSize_ShouldDeleteOldestFamiliesOnly()
    {
        await using var context = CreateDbContext();
        var user = await CreateUserAsync(context);
        var oldestFamilyId = Guid.NewGuid();
        var middleFamilyId = Guid.NewGuid();
        var newestFamilyId = Guid.NewGuid();

        context.RefreshTokens.AddRange(
            CreateToken(
                user.Id,
                oldestFamilyId,
                UtcNow.AddDays(-60),
                UtcNow.AddDays(-30)),
            CreateToken(
                user.Id,
                middleFamilyId,
                UtcNow.AddDays(-50),
                UtcNow.AddDays(-20)),
            CreateToken(
                user.Id,
                newestFamilyId,
                UtcNow.AddDays(-40),
                UtcNow.AddDays(-10)));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateCleanupService(context, batchSize: 2);

        var deletedCount = await service.CleanupAsync(
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var remainingFamilyIds = await context.RefreshTokens
            .Select(token => token.FamilyId)
            .ToListAsync();

        Assert.Equal(2, deletedCount);
        Assert.DoesNotContain(oldestFamilyId, remainingFamilyIds);
        Assert.DoesNotContain(middleFamilyId, remainingFamilyIds);
        Assert.Contains(newestFamilyId, remainingFamilyIds);
    }

    [Fact]
    public async Task CleanupAsync_WhenNoFamiliesExist_ShouldReturnZero()
    {
        await using var context = CreateDbContext();
        var service = CreateCleanupService(context, batchSize: 10);

        var deletedCount = await service.CleanupAsync(
            CancellationToken.None);

        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task CleanupAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        await using var context = CreateDbContext();
        var service = CreateCleanupService(context, batchSize: 10);
        using var cancellationTokenSource =
            new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CleanupAsync(cancellationTokenSource.Token));
    }

    private StreetcodeIdentityDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
                .UseSqlServer(_fixture.ConnectionString)
                .Options;

        return new StreetcodeIdentityDbContext(options);
    }

    private static RefreshTokenCleanupService CreateCleanupService(
        StreetcodeIdentityDbContext context,
        int batchSize)
    {
        var options = Options.Create(
            new RefreshTokenCleanupOptions
            {
                Interval = TimeSpan.FromHours(1),
                RetentionPeriod = RetentionPeriod,
                BatchSize = batchSize,
            });

        return new RefreshTokenCleanupService(
            context,
            new FixedTimeProvider(UtcNow),
            options);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        StreetcodeIdentityDbContext context)
    {
        var email = $"cleanup-{Guid.NewGuid():N}@example.com";
        var normalizedEmail = email.ToUpperInvariant();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private static (RefreshToken Original, RefreshToken Replacement)
        CreateRotatedFamily(
            Guid userId,
            Guid familyId,
            DateTimeOffset originalCreatedAt,
            DateTimeOffset originalExpiresAt,
            DateTimeOffset replacementCreatedAt,
            DateTimeOffset replacementExpiresAt)
    {
        var original = CreateToken(
            userId,
            familyId,
            originalCreatedAt,
            originalExpiresAt);
        var replacement = CreateToken(
            userId,
            familyId,
            replacementCreatedAt,
            replacementExpiresAt);

        original.Rotate(replacement.Id, replacementCreatedAt);

        return (original, replacement);
    }

    private static RefreshToken CreateToken(
        Guid userId,
        Guid familyId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            familyId,
            Guid.NewGuid().ToString("N"),
            createdAt,
            expiresAt);
    }

    private async Task DeleteAllRefreshTokensAsync()
    {
        await using var context = CreateDbContext();
        await context.RefreshTokens.ExecuteDeleteAsync();
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
