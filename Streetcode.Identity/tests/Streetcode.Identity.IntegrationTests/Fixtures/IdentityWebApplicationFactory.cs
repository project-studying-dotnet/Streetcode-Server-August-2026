using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;
using Streetcode.Identity.Infrastructure.Security;

namespace Streetcode.Identity.IntegrationTests.Fixtures;

public sealed class IdentityWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string ConnectionStringEnvironmentVariable =
        "STREETCODE_IDENTITY_ConnectionStrings__DefaultConnection";

    private static readonly object EnvironmentVariableLock = new();

    private readonly string _connectionString;

    public IdentityWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public new HttpClient CreateClient()
    {
        lock (EnvironmentVariableLock)
        {
            var previousValue = Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

            try
            {
                Environment.SetEnvironmentVariable(
                    ConnectionStringEnvironmentVariable,
                    _connectionString);

                return base.CreateClient();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    ConnectionStringEnvironmentVariable,
                    previousValue);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveHostedService<OutboxPublisherBackgroundService>(services);

            RemoveHostedService<RefreshTokenCleanupBackgroundService>(services);
        });
    }

    private static void RemoveHostedService<TService>(
        IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType == typeof(TService));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}
