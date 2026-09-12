using System.Data;
using Microsoft.Data.SqlClient;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Infrastructure;

[Collection(MsSqlCollection.Name)]
public class MsSqlContainerSmokeTests
{
    private readonly MsSqlContainerFixture _fixture;

    public MsSqlContainerSmokeTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Container_WhenStarted_ShouldAcceptSqlConnection()
    {
        await using var connection =
            new SqlConnection(_fixture.ConnectionString);

        await connection.OpenAsync();

        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.Equal(
            "StreetcodeIdentityIntegrationTests",
            connection.Database);
    }
}
