using Streetcode.Identity.Infrastructure.Security;

namespace Streetcode.Identity.UnitTests.Security;

public sealed class Sha256RefreshTokenHasherTests
{
    [Fact]
    public void ComputeHash_WhenTokenIsValid_ShouldReturnExpectedSha256Hash()
    {
        const string token = "abc";
        const string expectedHash =
            "BA7816BF8F01CFEA414140DE5DAE2223" +
            "B00361A396177A9CB410FF61F20015AD";
        var hasher = new Sha256RefreshTokenHasher();

        var hash = hasher.ComputeHash(token);

        Assert.Equal(expectedHash, hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void ComputeHash_WhenCalledForSameToken_ShouldReturnSameHash()
    {
        const string token = "same-refresh-token";
        var hasher = new Sha256RefreshTokenHasher();

        var firstHash = hasher.ComputeHash(token);
        var secondHash = hasher.ComputeHash(token);

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void ComputeHash_WhenCalledForDifferentTokens_ShouldReturnDifferentHashes()
    {
        var hasher = new Sha256RefreshTokenHasher();

        var firstHash = hasher.ComputeHash("first-refresh-token");
        var secondHash = hasher.ComputeHash("second-refresh-token");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void ComputeHash_WhenTokenIsNull_ShouldThrowArgumentNullException()
    {
        var hasher = new Sha256RefreshTokenHasher();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            hasher.ComputeHash(null!));

        Assert.Equal("token", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ComputeHash_WhenTokenIsEmptyOrWhitespace_ShouldThrowArgumentException(
        string token)
    {
        var hasher = new Sha256RefreshTokenHasher();

        var exception = Assert.Throws<ArgumentException>(() =>
            hasher.ComputeHash(token));

        Assert.Equal("token", exception.ParamName);
    }
}
