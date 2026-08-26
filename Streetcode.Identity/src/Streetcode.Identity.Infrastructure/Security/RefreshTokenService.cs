using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Domain.RefreshTokens;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const string InvalidTokenErrorCode = "RefreshToken.Invalid";
    private const string InvalidUserErrorCode = "RefreshToken.InvalidUser";

    private readonly StreetcodeIdentityDbContext _dbContext;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly RefreshTokenOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenService(
        StreetcodeIdentityDbContext dbContext,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IOptions<RefreshTokenOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RefreshTokenResult>> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty)
        {
            return InvalidUserFailure();
        }

        var canReceiveToken = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId && user.IsActive,
                cancellationToken);

        if (!canReceiveToken)
        {
            return InvalidUserFailure();
        }

        var now = _timeProvider.GetUtcNow();
        var rawToken = _refreshTokenGenerator.Generate();
        var tokenHash = _refreshTokenHasher.ComputeHash(rawToken);

        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            Guid.NewGuid(),
            tokenHash,
            now,
            now.Add(_options.Lifetime));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(ToResult(refreshToken, rawToken));
    }

    public async Task<Result<RefreshTokenResult>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidTokenFailure();
        }

        var tokenHash = _refreshTokenHasher.ComputeHash(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);

        if (storedToken is null)
        {
            return InvalidTokenFailure();
        }

        var now = _timeProvider.GetUtcNow();

        if (storedToken.RevokedAt is not null)
        {
            if (storedToken.ReplacedByTokenId is not null)
            {
                await RevokeFamilyByIdAsync(
                    storedToken.FamilyId,
                    now,
                    cancellationToken);
            }

            return InvalidTokenFailure();
        }

        if (storedToken.IsExpiredAt(now))
        {
            return InvalidTokenFailure();
        }

        var userIsActive = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == storedToken.UserId && user.IsActive,
                cancellationToken);

        if (!userIsActive)
        {
            await RevokeFamilyByIdAsync(
                storedToken.FamilyId,
                now,
                cancellationToken);

            return InvalidTokenFailure();
        }

        var replacementRawToken = _refreshTokenGenerator.Generate();
        var replacementToken = RefreshToken.Create(
            Guid.NewGuid(),
            storedToken.UserId,
            storedToken.FamilyId,
            _refreshTokenHasher.ComputeHash(replacementRawToken),
            now,
            now.Add(_options.Lifetime));

        storedToken.Rotate(replacementToken.Id, now);
        _dbContext.RefreshTokens.Add(replacementToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();

            await RevokeFamilyByIdAsync(
                storedToken.FamilyId,
                now,
                cancellationToken);

            return InvalidTokenFailure();
        }

        return Result.Ok(ToResult(replacementToken, replacementRawToken));
    }

    public async Task<Result> RevokeFamilyAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidTokenFailure();
        }

        var tokenHash = _refreshTokenHasher.ComputeHash(refreshToken);
        var familyId = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return InvalidTokenFailure();
        }

        await RevokeFamilyByIdAsync(
            familyId.Value,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return Result.Ok();
    }

    private async Task RevokeFamilyByIdAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        var familyTokens = await _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId)
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        foreach (var token in familyTokens)
        {
            hasChanges |= token.Revoke(revokedAt);
        }

        if (hasChanges)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.ChangeTracker.Clear();
            }
        }
    }

    private static RefreshTokenResult ToResult(
        RefreshToken refreshToken,
        string rawToken)
    {
        return new RefreshTokenResult(
            refreshToken.UserId,
            rawToken,
            refreshToken.ExpiresAt);
    }

    private static Result<RefreshTokenResult> InvalidUserFailure()
    {
        return Result.Fail<RefreshTokenResult>(
            CreateError(
                "The user cannot receive a refresh token",
                InvalidUserErrorCode));
    }

    private static Error InvalidTokenFailure()
    {
        return CreateError(
            "The refresh token is invalid or inactive",
            InvalidTokenErrorCode);
    }

    private static Error CreateError(string message, string code)
    {
        return new Error(message)
            .WithMetadata("Code", code);
    }
}
