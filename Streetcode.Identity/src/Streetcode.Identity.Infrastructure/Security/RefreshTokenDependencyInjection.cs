using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Infrastructure.Security;

public static class RefreshTokenDependencyInjection
{
    public static IServiceCollection AddRefreshTokenServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(
                RefreshTokenOptions.SectionName))
            .Validate(
                options => options.Lifetime > TimeSpan.Zero,
                "Refresh token lifetime must be greater than zero")
            .ValidateOnStart();

        services.AddOptions<RefreshTokenCleanupOptions>()
            .Bind(configuration.GetSection(RefreshTokenCleanupOptions.SectionName))
            .Validate(options => options.Interval > TimeSpan.Zero,
                "Refresh token cleanup interval must be greater than zero")
            .Validate(options => options.RetentionPeriod > TimeSpan.Zero,
                "Refresh token retention period must be greater than zero")
            .Validate(options => options.BatchSize > 0 && options.BatchSize <= 1000,
                "Refresh token cleanup batch size must be between 1 and 1000")
            .ValidateOnStart();

        services.AddSingleton<
            IRefreshTokenGenerator,
            CryptographicRefreshTokenGenerator>();

        services.AddSingleton<
            IRefreshTokenHasher,
            Sha256RefreshTokenHasher>();

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<RefreshTokenCleanupService>();

        services.AddHostedService<RefreshTokenCleanupBackgroundService>();

        return services;
    }
}
