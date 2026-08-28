using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Infrastructure.Identity.Jwt;
using System.Text;

namespace Streetcode.Identity.WebApi.Extensions
{
    public static class JwtServiceExtensions
    {
        public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);

            services.AddOptions<JwtOptions>()
                .Bind(jwtSection)
                .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
                .Validate(o => o.LifetimeInMinutes > 0, "Jwt:LifetimeInMinutes must be positive.")
                .Validate(
                    o => !string.IsNullOrWhiteSpace(o.SecretKey) && Encoding.UTF8.GetByteCount(o.SecretKey) >= 32,
                    "Jwt:SecretKey must be set and at least 32 bytes (256 bits) for HS256.")
                .ValidateOnStart();

            services.AddScoped<IJwtService, JwtService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection["Issuer"],

                        ValidateAudience = true,
                        ValidAudience = jwtSection["Audience"],

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSection["SecretKey"] ?? string.Empty)),
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
