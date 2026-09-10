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

    private const string JwtSecretEnvironmentVariable =
        "STREETCODE_IDENTITY_Jwt__SecretKey";

    private const string TestJwtSecret =
        "Integration_Test_Jwt_Secret_Key_At_Least_32_Bytes!";

    private static readonly object EnvironmentVariableLock = new();

    private readonly string _connectionString;
    private readonly Action<IServiceCollection>? _configureServices;

    public IdentityWebApplicationFactory(
        string connectionString,
        Action<IServiceCollection>? configureServices = null)
    {
        _connectionString = connectionString;
        _configureServices = configureServices;
    }

    public new HttpClient CreateClient()
    {
        lock (EnvironmentVariableLock)
        {
            var previousValue = Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

            var previousJwtSecret = Environment.GetEnvironmentVariable(
                JwtSecretEnvironmentVariable);

            try
            {
                Environment.SetEnvironmentVariable(
                    ConnectionStringEnvironmentVariable,
                    _connectionString);

                Environment.SetEnvironmentVariable(
                    JwtSecretEnvironmentVariable,
                    TestJwtSecret);

                return base.CreateClient();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    ConnectionStringEnvironmentVariable,
                    previousValue);

                Environment.SetEnvironmentVariable(
                    JwtSecretEnvironmentVariable,
                    previousJwtSecret);
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

            _configureServices?.Invoke(services);
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
