using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.WebApi;

[Collection(MsSqlCollection.Name)]
public sealed class LogoutEndpointIntegrationTests
{
    private readonly MsSqlContainerFixture _fixture;

    public LogoutEndpointIntegrationTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Logout_WhenRefreshTokenDoesNotExist_ShouldReturnNoContent()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { RefreshToken = "unknown-refresh-token" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_WhenRefreshTokenIsEmpty_ShouldReturnValidationProblem()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { RefreshToken = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorCode = problem
            .GetProperty("errorCodes")
            .GetProperty("RefreshToken")[0]
            .GetString();

        Assert.Equal("RefreshToken.Required", errorCode);
    }

    private IdentityWebApplicationFactory CreateFactory()
    {
        return new IdentityWebApplicationFactory(
            _fixture.ConnectionString);
    }
}
