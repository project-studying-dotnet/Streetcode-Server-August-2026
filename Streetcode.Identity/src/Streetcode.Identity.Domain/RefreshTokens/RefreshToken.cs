namespace Streetcode.Identity.Domain.RefreshTokens;

public sealed class RefreshToken
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public long ConcurrencyVersion { get; private set; } = 1;

    public static RefreshToken Create(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        EnsureNotEmpty(id, nameof(id));
        EnsureNotEmpty(userId, nameof(userId));
        EnsureNotEmpty(familyId, nameof(familyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Refresh token expiration must be later than its creation time");
        }

        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        };
    }

    public bool IsActiveAt(DateTimeOffset timestamp)
    {
        return timestamp >= CreatedAt &&
               timestamp < ExpiresAt &&
               RevokedAt is null;
    }

    public bool IsExpiredAt(DateTimeOffset timestamp)
    {
        return timestamp >= ExpiresAt;
    }

    public bool Revoke(DateTimeOffset revokedAt)
    {
        EnsureTimestampIsNotBeforeCreation(revokedAt, nameof(revokedAt));

        if (RevokedAt is not null)
        {
            return false;
        }

        RevokedAt = revokedAt;
        IncrementConcurrencyVersion();

        return true;
    }

    public void Rotate(Guid replacementTokenId, DateTimeOffset rotatedAt)
    {
        EnsureNotEmpty(replacementTokenId, nameof(replacementTokenId));
        EnsureTimestampIsNotBeforeCreation(rotatedAt, nameof(rotatedAt));

        if (replacementTokenId == Id)
        {
            throw new ArgumentException(
                "A refresh token cannot replace itself",
                nameof(replacementTokenId));
        }

        if (!IsActiveAt(rotatedAt))
        {
            throw new InvalidOperationException(
                "Only an active refresh token can be rotated");
        }

        RevokedAt = rotatedAt;
        ReplacedByTokenId = replacementTokenId;
        IncrementConcurrencyVersion();
    }

    private static void EnsureNotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier must not be empty",
                parameterName);
        }
    }

    private void EnsureTimestampIsNotBeforeCreation(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Timestamp must not be earlier than the refresh token creation time");
        }
    }

    private void IncrementConcurrencyVersion()
    {
        ConcurrencyVersion = checked(ConcurrencyVersion + 1);
    }
}
