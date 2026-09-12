using Microsoft.Extensions.Configuration;
using Streetcode.WebApi.Extensions;
using Xunit;

namespace Streetcode.XUnitTest.Extensions.ConfigurationBuilder;

public class ConfigurationBuilderExtensionsTests
{
    [Fact]
    public void GetRequiredConnectionString_WhenConnectionStringIsMissing_ShouldThrowInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredConnectionString());
        Assert.Equal(TestMessages.MissingDefaultConnectionString, exception.Message);
    }

    [Fact]
    public void GetRequiredConnectionString_WhenConnectionStringExists_ShouldReturnConnectionString()
    {
        const string expectedConnectionString =
            "Server=localhost;Database=Streetcode;Trusted_Connection=True;";

        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:DefaultConnection"] = expectedConnectionString;

        var actualConnectionString = configuration.GetRequiredConnectionString();
        Assert.Equal(expectedConnectionString, actualConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRequiredConnectionString_WhenConnectionStringIsEmptyOrWhitespace_ShouldThrowInvalidOperationException(
        string invalidConnectionString)
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:DefaultConnection"] = invalidConnectionString;

        var exception = Assert.Throws<InvalidOperationException>(
            () => configuration.GetRequiredConnectionString());

        Assert.Contains(TestMessages.DefaultConnectionConfigurationKey, exception.Message);
    }
}
