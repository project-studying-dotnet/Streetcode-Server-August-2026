using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.Infrastructure;

public static class DependencyInjection
{
    private const string DummyPassword =
        "Streetcode.Identity.DummyPassword";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<StreetcodeIdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<StreetcodeIdentityDbContext>()
            .AddSignInManager();

        services.AddSingleton<DummyPasswordHash>(serviceProvider =>
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var hash = userManager.PasswordHasher.HashPassword(
                new ApplicationUser(),
                DummyPassword);

            return new DummyPasswordHash(hash);
        });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
