using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

public sealed class GatewayRoutingTests
{
    [Fact]
    public async Task Health_returns_ok()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/login?next=home", "identity")]
    [InlineData("/api/streetcode/getall?limit=5", "streetcode")]
    public async Task Routes_requests_to_the_correct_service(string path, string expectedBackend)
    {
        await using var identity = await StartBackendAsync("identity");
        await using var streetcode = await StartBackendAsync("streetcode");
        await using var factory = CreateGateway(identity.Address, streetcode.Address);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent("payload"),
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:3000");

        using var response = await client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedBackend, body.RootElement.GetProperty("backend").GetString());
        Assert.Equal(path, body.RootElement.GetProperty("path").GetString());
        Assert.Equal("POST", body.RootElement.GetProperty("method").GetString());
        Assert.Equal("payload", body.RootElement.GetProperty("body").GetString());
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Cors_preflight_is_handled_by_the_gateway()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:3000");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    private static WebApplicationFactory<Program> CreateGateway(string identityAddress, string streetcodeAddress) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:identity:Destinations:identity-api:Address"] = identityAddress,
                    ["ReverseProxy:Clusters:streetcode:Destinations:api:Address"] = streetcodeAddress,
                })));

    private static async Task<MockBackend> StartBackendAsync(string name)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();
        app.Run(async context =>
        {
            if (name == "streetcode")
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:3000";
            }

            await context.Response.WriteAsJsonAsync(new
            {
                backend = name,
                path = context.Request.Path + context.Request.QueryString,
                method = context.Request.Method,
                body = await new StreamReader(context.Request.Body).ReadToEndAsync(),
            });
        });
        await app.StartAsync();

        var address = app.Urls.Single();
        return new MockBackend(app, address);
    }

    private sealed class MockBackend(WebApplication app, string address) : IAsyncDisposable
    {
        public string Address { get; } = address;

        public async ValueTask DisposeAsync() => await app.DisposeAsync();
    }
}
