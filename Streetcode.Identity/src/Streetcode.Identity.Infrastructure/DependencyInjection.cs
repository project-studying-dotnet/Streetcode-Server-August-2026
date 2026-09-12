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

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
