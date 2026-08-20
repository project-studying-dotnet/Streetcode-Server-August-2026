using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Streetcode.BLL.Interfaces.Jwt;
using Streetcode.BLL.Services.Jwt;
using System.Runtime.CompilerServices;
using System.Text;

namespace Streetcode.WebApi.Extensions
{
    public static class JwtServiceExtensions
    {
        public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);
            services.Configure<JwtOptions>(jwtSection);
            services.AddScoped<IJwtService, JwtService>();

            var jwtOptions = jwtSection.Get<JwtOptions>();

            if (jwtOptions is null || string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            {
                throw new InvalidOperationException(
                    $"Configuration section '{JwtOptions.SectionName}' or its SecretKey is missing. " +
                    "Set Jwt:SecretKey via an environment variable or secret store.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey!))
                    };
                });

            return services;
        }
    }
}
