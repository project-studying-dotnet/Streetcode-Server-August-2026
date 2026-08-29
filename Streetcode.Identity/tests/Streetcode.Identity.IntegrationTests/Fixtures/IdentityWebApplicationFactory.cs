using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

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
            var outboxBackgroundService = services.SingleOrDefault(
                descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType ==
                    typeof(OutboxPublisherBackgroundService));

            if (outboxBackgroundService is not null)
            {
                services.Remove(outboxBackgroundService);
            }
        });
    }
}
