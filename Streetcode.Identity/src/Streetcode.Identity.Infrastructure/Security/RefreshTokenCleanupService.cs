using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.Infrastructure.Security;

public sealed class RefreshTokenCleanupService
{
    private readonly StreetcodeIdentityDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly RefreshTokenCleanupOptions _options;

    public RefreshTokenCleanupService(
        StreetcodeIdentityDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<RefreshTokenCleanupOptions> options)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<int> CleanupAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cutoff = _timeProvider.GetUtcNow() - _options.RetentionPeriod;

        var familyIdsToDelete = await _dbContext.RefreshTokens
            .GroupBy(token => token.FamilyId)
            .Where(family =>
                family.Max(token => token.ExpiresAt) <= cutoff)
            .OrderBy(family =>
                family.Max(token => token.ExpiresAt))
            .Select(family => family.Key)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (familyIdsToDelete.Count == 0)
        {
            return 0;
        }

        return await _dbContext.RefreshTokens
            .Where(token =>
                familyIdsToDelete.Contains(token.FamilyId))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
