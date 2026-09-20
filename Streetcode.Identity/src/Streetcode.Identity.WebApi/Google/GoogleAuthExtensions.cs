using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Streetcode.Identity.Application.Abstractions;

namespace Streetcode.Identity.WebApi.Google;

public static class GoogleAuthExtensions
{
    public static IServiceCollection AddGoogleAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleAuthOptions>(configuration.GetSection("GoogleAuth"));
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
            new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true }));
        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
        return services;
    }
}
