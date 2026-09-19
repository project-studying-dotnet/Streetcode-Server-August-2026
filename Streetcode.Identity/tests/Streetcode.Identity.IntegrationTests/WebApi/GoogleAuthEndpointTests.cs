using System.Net;
using System.Net.Http.Json;
using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Streetcode.Identity.Application;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Google;
using Streetcode.Identity.WebApi.Controllers;
using Streetcode.Identity.WebApi.ExceptionHandling;
using Streetcode.Identity.WebApi.Extensions;

namespace Streetcode.Identity.IntegrationTests.WebApi;

public sealed class GoogleAuthEndpointTests
{
    [Fact]
    public async Task Link_WithoutStreetcodeBearerToken_Returns401()
    {
        await using var app = await App();
        var response = await app.GetTestClient().PostAsJsonAsync("/api/auth/google/link",
            new LinkGoogleRequest("google-token", "Password123!", true));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.BadRequest)]
    [InlineData(true, HttpStatusCode.NoContent)]
    public async Task Link_WithValidBearer_RequiresSeparateConfirmation(bool confirm, HttpStatusCode expected)
    {
        await using var app = await App();
        var client = app.GetTestClient();
        using var scope = app.Services.CreateScope();
        var userId = Guid.NewGuid();
        var token = scope.ServiceProvider.GetRequiredService<IJwtService>()
            .GenerateToken(userId, "user@example.com", ["User"], 7);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);

        var response = await client.PostAsJsonAsync("/api/auth/google/link",
            new LinkGoogleRequest("google-token", "Password123!", confirm));

        Assert.Equal(expected, response.StatusCode);
        var identity = app.Services.GetRequiredService<FakeIdentity>();
        Assert.Equal(confirm ? userId : null, identity.LinkedUser);
        if (confirm) Assert.Equal(7, identity.LinkedVersion);
    }

    [Fact]
    public async Task Login_EmailCollision_Returns409WithMachineReadableCode()
    {
        await using var app = await App();
        var response = await app.GetTestClient().PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest("google-token"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Google.LinkRequired", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_FormPost_IsRejected()
    {
        await using var app = await App();
        var response = await app.GetTestClient().PostAsync("/api/auth/google",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["idToken"] = "google-token" }));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private static async Task<WebApplication> App()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "StreetcodeServer",
            ["Jwt:Audience"] = "StreetcodeClient",
            ["Jwt:SecretKey"] = "Only_For_Tests_A_Long_Secret_Key_123456789",
            ["Jwt:LifetimeInMinutes"] = "10",
        });
        builder.Services.AddJwtServices(builder.Configuration);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddApplication();
        builder.Services.AddSingleton<IGoogleTokenVerifier, FakeVerifier>();
        builder.Services.AddSingleton<FakeIdentity>();
        builder.Services.AddSingleton<IGoogleIdentityService>(sp => sp.GetRequiredService<FakeIdentity>());
        builder.Services.AddSingleton<IRefreshTokenService, NeverIssueTokens>();
        builder.Services.AddControllers().AddApplicationPart(typeof(GoogleAuthController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private sealed class FakeVerifier : IGoogleTokenVerifier
    {
        public Task<Result<GoogleAccount>> VerifyAsync(string idToken, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Ok(new GoogleAccount("subject", "user@example.com")));
    }

    private sealed class FakeIdentity : IGoogleIdentityService
    {
        public Guid? LinkedUser { get; private set; }
        public long LinkedVersion { get; private set; }
        public Task<Result<UserTokenData>> AuthenticateAsync(GoogleAccount account, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Fail<UserTokenData>(GoogleAuthErrors.LinkRequired()));
        public Task<Result> LinkAsync(GoogleAccount account, Guid userId, long accessVersion,
            string password, CancellationToken cancellationToken)
        {
            LinkedUser = userId;
            LinkedVersion = accessVersion;
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class NeverIssueTokens : IRefreshTokenService
    {
        public Task<Result<RefreshTokenResult>> IssueAsync(Guid userId, CancellationToken cancellationToken) => throw new InvalidOperationException("Must not issue a session on email collision");
        public Task<Result<RefreshTokenResult>> RotateAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> RevokeFamilyAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
