using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Infrastructure.Persistence;
using Streetcode.Email.Infrastructure.Persistence.Repositories;

namespace Streetcode.Email.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EmailDbContext>(
            options => options.UseSqlServer(connectionString));

        services.AddScoped<IEmailDeliveryRepository, EmailDeliveryRepository>();

        return services;
    }
}
