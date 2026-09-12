using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Streetcode.Identity.IntegrationTests.Fixtures;

public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    private const string MsSqlImage =
        "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    private readonly MsSqlContainer _container =
        new MsSqlBuilder(MsSqlImage)
            .WithDatabase("StreetcodeIdentityIntegrationTests")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new StreetcodeIdentityDbContext(options);

        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
