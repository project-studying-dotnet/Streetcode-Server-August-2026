using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application.Common.Authorization;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.WebApi;

[Collection(MsSqlCollection.Name)]
public sealed class ProductionStartupIntegrationTests
{
    private readonly MsSqlContainerFixture _fixture;

    public ProductionStartupIntegrationTests(
        MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Startup_WhenProductionDatabaseIsEmpty_ShouldMigrateSeedRoles()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(
            _fixture.ConnectionString)
        {
            InitialCatalog =
                $"IdentityProductionStartup_{Guid.NewGuid():N}",
        };
        
        var connectionString =
            connectionStringBuilder.ConnectionString;

        await using var factory = new IdentityWebApplicationFactory(
            connectionString,
            environment: "Production");
        
        using var client = factory.CreateClient();

        var options =
            new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
                .UseSqlServer(connectionString)
                .Options;
        
        await using var dbContext =
            new StreetcodeIdentityDbContext(options);
        
        var appliedMigrations =
            await dbContext.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(appliedMigrations);
        
        Assert.True(await dbContext.Roles.AnyAsync(
            role => role.Name == RoleNames.User));
        
        Assert.True(await dbContext.Roles.AnyAsync(
            role => role.Name == RoleNames.Admin));
    }
}