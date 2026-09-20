using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.Email.Infrastructure.Persistence;
using Streetcode.Email.IntegrationTests.Fixtures;

namespace Streetcode.Email.IntegrationTests.Persistence;

[Collection(EmailInfrastructureCollection.Name)]
public sealed class DatabaseMigrationTests
{
    private const string InitialMigration =
        "20260908163331_InitialEmailSchema";

    private readonly EmailInfrastructureFixture _fixture;

    public DatabaseMigrationTests(
        EmailInfrastructureFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task Startup_WhenDatabaseIsEmpty_AppliesMigrations()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(
            _fixture.DatabaseConnectionString)
        {
            InitialCatalog = $"StreetcodeEmail_{Guid.NewGuid():N}",
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        await using var factory =
            _fixture.CreateApplicationFactory(connectionString);

        using var client = factory.CreateClient();

        var options = new DbContextOptionsBuilder<EmailDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new EmailDbContext(options);

        var appliedMigrations = await dbContext.Database
            .GetAppliedMigrationsAsync();

        var deliveryCount = await dbContext.EmailDeliveries
            .CountAsync();

        Assert.Contains(InitialMigration, appliedMigrations);
        Assert.Equal(0, deliveryCount);
    }
}
