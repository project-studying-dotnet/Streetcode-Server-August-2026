using Streetcode.Identity.Infrastructure.Security;

namespace Streetcode.Identity.UnitTests.Security;

public sealed class CryptographicRefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_WhenCalled_ShouldReturnSixtyFourHexCharacters()
    {
        var generator = new CryptographicRefreshTokenGenerator();

        var token = generator.Generate();

        Assert.Equal(64, token.Length);
        Assert.All(
            token,
            character => Assert.True(char.IsAsciiHexDigit(character)));
    }

    [Fact]
    public void Generate_WhenCalledTwice_ShouldReturnDifferentTokens()
    {
        var generator = new CryptographicRefreshTokenGenerator();

        var firstToken = generator.Generate();
        var secondToken = generator.Generate();

        Assert.NotEqual(firstToken, secondToken);
    }
}
