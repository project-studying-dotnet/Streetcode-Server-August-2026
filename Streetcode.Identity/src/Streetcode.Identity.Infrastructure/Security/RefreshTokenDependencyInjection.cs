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

        services.AddSingleton<
            IRefreshTokenGenerator,
            CryptographicRefreshTokenGenerator>();

        services.AddSingleton<
            IRefreshTokenHasher,
            Sha256RefreshTokenHasher>();

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
