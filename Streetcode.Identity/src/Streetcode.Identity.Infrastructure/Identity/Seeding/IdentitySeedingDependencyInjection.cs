using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Streetcode.Identity.Infrastructure.Identity.Seeding;

public static class IdentitySeedingDependencyInjection
{
    public static IServiceCollection AddIdentitySeeding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<IdentitySeedOptions>()
            .Bind(configuration.GetSection(IdentitySeedOptions.SectionName))
            .Validate(
                options =>
                    !options.Enabled ||
                    !string.IsNullOrWhiteSpace(options.AdminEmail),
                "IdentitySeed:AdminEmail is required when identity seeding is enabled")
            .Validate(
                options =>
                    !options.Enabled ||
                    !string.IsNullOrWhiteSpace(options.AdminPassword),
                "IdentitySeed:AdminPassword is required when identity seeding is enabled")
            .ValidateOnStart();

        services.AddScoped<IdentityDataSeeder>();

        return services;
    }
}
