using System.Net;
using Streetcode.Email.IntegrationTests.Fixtures;

namespace Streetcode.Email.IntegrationTests.HealthChecks;

[Collection(EmailInfrastructureCollection.Name)]
public sealed class HealthCheckTests
{
    private readonly EmailInfrastructureFixture _fixture;

    public HealthCheckTests(
        EmailInfrastructureFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task Live_WhenApplicationIsRunning_ReturnsHealthy()
    {
        await using var factory =
            _fixture.CreateApplicationFactory();

        using var client = factory.CreateClient();

        using var response =
            await client.GetAsync("/health/live");

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task Ready_WhenDependenciesAreAvailable_ReturnsHealthy()
    {
        await using var factory =
            _fixture.CreateApplicationFactory();

        using var client = factory.CreateClient();

        using var response =
            await client.GetAsync("/health/ready");

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal("Healthy", content);
    }
}
