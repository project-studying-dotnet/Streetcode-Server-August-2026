using Streetcode.Identity.Domain.RefreshTokens;

namespace Streetcode.Identity.UnitTests.Domain.RefreshTokens;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiresAt =
        CreatedAt.AddDays(30);

    [Fact]
    public void Create_WhenArgumentsAreValid_ShouldInitializeActiveToken()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        const string tokenHash = "valid-token-hash";

        var token = RefreshToken.Create(
            id,
            userId,
            familyId,
            tokenHash,
            CreatedAt,
            ExpiresAt);

        Assert.Equal(id, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(familyId, token.FamilyId);
        Assert.Equal(tokenHash, token.TokenHash);
        Assert.Equal(CreatedAt, token.CreatedAt);
        Assert.Equal(ExpiresAt, token.ExpiresAt);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
        Assert.Equal(1L, token.ConcurrencyVersion);
        Assert.True(token.IsActiveAt(CreatedAt));
    }

    [Fact]
    public void Create_WhenIdIsEmpty_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "valid-token-hash",
                CreatedAt,
                ExpiresAt));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                "valid-token-hash",
                CreatedAt,
                ExpiresAt));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Create_WhenFamilyIdIsEmpty_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                "valid-token-hash",
                CreatedAt,
                ExpiresAt));

        Assert.Equal("familyId", exception.ParamName);
    }

    [Fact]
    public void Create_WhenTokenHashIsNull_ShouldThrowArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null!,
                CreatedAt,
                ExpiresAt));

        Assert.Equal("tokenHash", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WhenTokenHashIsEmptyOrWhitespace_ShouldThrowArgumentException(
        string tokenHash)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                tokenHash,
                CreatedAt,
                ExpiresAt));

        Assert.Equal("tokenHash", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenExpirationIsNotLaterThanCreation_ShouldThrowArgumentOutOfRangeException(
        int expirationOffsetInSeconds)
    {
        var invalidExpiration = CreatedAt.AddSeconds(expirationOffsetInSeconds);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "valid-token-hash",
                CreatedAt,
                invalidExpiration));

        Assert.Equal("expiresAt", exception.ParamName);
    }

    [Fact]
    public void IsActiveAt_WhenTimestampIsBeforeCreation_ShouldReturnFalse()
    {
        var token = CreateToken();

        var isActive = token.IsActiveAt(CreatedAt.AddTicks(-1));

        Assert.False(isActive);
    }

    [Fact]
    public void IsActiveAt_WhenTimestampIsWithinLifetime_ShouldReturnTrue()
    {
        var token = CreateToken();

        var isActive = token.IsActiveAt(CreatedAt.AddDays(1));

        Assert.True(isActive);
    }

    [Fact]
    public void IsExpiredAt_WhenTimestampReachesExpiration_ShouldReturnTrue()
    {
        var token = CreateToken();

        Assert.False(token.IsExpiredAt(ExpiresAt.AddTicks(-1)));
        Assert.True(token.IsExpiredAt(ExpiresAt));
        Assert.False(token.IsActiveAt(ExpiresAt));
    }

    [Fact]
    public void Revoke_WhenTokenIsNotRevoked_ShouldRevokeAndIncrementConcurrencyVersion()
    {
        var token = CreateToken();
        var revokedAt = CreatedAt.AddDays(1);

        var wasRevoked = token.Revoke(revokedAt);

        Assert.True(wasRevoked);
        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.False(token.IsActiveAt(revokedAt));
        Assert.Equal(2L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Revoke_WhenTokenIsAlreadyRevoked_ShouldNotChangeStateAgain()
    {
        var token = CreateToken();
        var firstRevokedAt = CreatedAt.AddDays(1);
        token.Revoke(firstRevokedAt);

        var wasRevokedAgain = token.Revoke(CreatedAt.AddDays(2));

        Assert.False(wasRevokedAgain);
        Assert.Equal(firstRevokedAt, token.RevokedAt);
        Assert.Equal(2L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Revoke_WhenTimestampIsBeforeCreation_ShouldThrowArgumentOutOfRangeException()
    {
        var token = CreateToken();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            token.Revoke(CreatedAt.AddTicks(-1)));

        Assert.Equal("revokedAt", exception.ParamName);
        Assert.Null(token.RevokedAt);
        Assert.Equal(1L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Rotate_WhenTokenIsActive_ShouldRevokeLinkReplacementAndIncrementConcurrencyVersion()
    {
        var token = CreateToken();
        var replacementTokenId = Guid.NewGuid();
        var rotatedAt = CreatedAt.AddDays(1);

        token.Rotate(replacementTokenId, rotatedAt);

        Assert.Equal(rotatedAt, token.RevokedAt);
        Assert.Equal(replacementTokenId, token.ReplacedByTokenId);
        Assert.False(token.IsActiveAt(rotatedAt));
        Assert.Equal(2L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Rotate_WhenReplacementIdIsEmpty_ShouldThrowArgumentException()
    {
        var token = CreateToken();

        var exception = Assert.Throws<ArgumentException>(() =>
            token.Rotate(Guid.Empty, CreatedAt.AddDays(1)));

        Assert.Equal("replacementTokenId", exception.ParamName);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public void Rotate_WhenReplacementIdEqualsCurrentTokenId_ShouldThrowArgumentException()
    {
        var token = CreateToken();

        var exception = Assert.Throws<ArgumentException>(() =>
            token.Rotate(token.Id, CreatedAt.AddDays(1)));

        Assert.Equal("replacementTokenId", exception.ParamName);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public void Rotate_WhenTimestampIsBeforeCreation_ShouldThrowArgumentOutOfRangeException()
    {
        var token = CreateToken();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            token.Rotate(Guid.NewGuid(), CreatedAt.AddTicks(-1)));

        Assert.Equal("rotatedAt", exception.ParamName);
        Assert.Equal(1L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Rotate_WhenTokenIsExpired_ShouldThrowInvalidOperationException()
    {
        var token = CreateToken();

        Assert.Throws<InvalidOperationException>(() =>
            token.Rotate(Guid.NewGuid(), ExpiresAt));

        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
        Assert.Equal(1L, token.ConcurrencyVersion);
    }

    [Fact]
    public void Rotate_WhenTokenIsRevoked_ShouldThrowInvalidOperationException()
    {
        var token = CreateToken();
        token.Revoke(CreatedAt.AddDays(1));

        Assert.Throws<InvalidOperationException>(() =>
            token.Rotate(Guid.NewGuid(), CreatedAt.AddDays(2)));

        Assert.Null(token.ReplacedByTokenId);
        Assert.Equal(2L, token.ConcurrencyVersion);
    }

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "valid-token-hash",
            CreatedAt,
            ExpiresAt);
    }
}
