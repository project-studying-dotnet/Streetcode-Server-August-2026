using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Infrastructure.Identity.Seeding;

namespace Streetcode.Identity.UnitTests.Identity.Seeding;

public sealed class IdentitySeedingDependencyInjectionTests
{
    [Fact]
    public void Options_WhenEnabledAndConfigurationIsValid_ShouldBindValues()
    {
        const string adminEmail = "admin@example.com";
        const string adminPassword = "ValidAdminPassword123!";

        using var serviceProvider = CreateServiceProvider(
            enabled: true,
            adminEmail,
            adminPassword);

        var options = serviceProvider
            .GetRequiredService<IOptions<IdentitySeedOptions>>()
            .Value;

        Assert.True(options.Enabled);
        Assert.Equal(adminEmail, options.AdminEmail);
        Assert.Equal(adminPassword, options.AdminPassword);
    }

    [Fact]
    public void Options_WhenEnabledAndAdminEmailIsInvalid_ShouldFailValidation()
    {
        const string invalidEmail = "not-an-email";

        using var serviceProvider = CreateServiceProvider(
            enabled: true,
            invalidEmail,
            "ValidAdminPassword123!");

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider
                .GetRequiredService<IOptions<IdentitySeedOptions>>()
                .Value);

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(
                "valid email address",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Options_WhenDisabledAndCredentialsAreEmpty_ShouldSucceed()
    {
        using var serviceProvider = CreateServiceProvider(
            enabled: false,
            string.Empty,
            string.Empty);

        var options = serviceProvider
            .GetRequiredService<IOptions<IdentitySeedOptions>>()
            .Value;

        Assert.False(options.Enabled);
        Assert.Empty(options.AdminEmail);
        Assert.Empty(options.AdminPassword);
    }

    private static ServiceProvider CreateServiceProvider(
        bool enabled,
        string adminEmail,
        string adminPassword)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{IdentitySeedOptions.SectionName}:Enabled"] =
                enabled.ToString(),
            [$"{IdentitySeedOptions.SectionName}:AdminEmail"] =
                adminEmail,
            [$"{IdentitySeedOptions.SectionName}:AdminPassword"] =
                adminPassword,
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();

        services.AddIdentitySeeding(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
