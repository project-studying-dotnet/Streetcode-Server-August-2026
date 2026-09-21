using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Streetcode.Identity.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Streetcode.Identity.IntegrationTests.Fixtures;

public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    private const string MsSqlImage =
        "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    private readonly MsSqlContainer? _container;
    private readonly string? _localConnectionString;

    public MsSqlContainerFixture()
    {
        var localServer = Environment.GetEnvironmentVariable("STREETCODE_IDENTITY_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(localServer))
        {
            _container = new MsSqlBuilder(MsSqlImage)
                .WithDatabase("StreetcodeIdentityIntegrationTests")
                .Build();
        }
        else
        {
            // Never use a supplied application database. Each run owns a fresh test database.
            _localConnectionString = new SqlConnectionStringBuilder(localServer)
            {
                InitialCatalog = $"StreetcodeIdentityTests_{Guid.NewGuid():N}",
            }.ConnectionString;
        }
    }

    public string ConnectionString =>
        _localConnectionString ?? _container!.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (_container is not null) await _container.StartAsync();

        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new StreetcodeIdentityDbContext(options);

        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        else
        {
            var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
                .UseSqlServer(ConnectionString).Options;
            await using var context = new StreetcodeIdentityDbContext(options);
            await context.Database.EnsureDeletedAsync();
        }
    }
}
