using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Streetcode.Identity.WebApi.Google;

namespace Streetcode.Identity.IntegrationTests.WebApi;

public sealed class GoogleTokenVerifierTests : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private const string ClientId = "test.apps.googleusercontent.com";

    [Fact]
    public async Task Verify_ValidSignedToken_ReturnsSubjectAndEmail()
    {
        var result = await Verifier().VerifyAsync(Token(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("google-subject", result.Value.Subject);
        Assert.Equal("member@example.com", result.Value.Email);
    }

    [Theory]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("expired")]
    [InlineData("signature")]
    [InlineData("unverified")]
    [InlineData("subject")]
    [InlineData("email")]
    public async Task Verify_InvalidClaimsOrSignature_ReturnsFailure(string scenario)
    {
        var result = await Verifier().VerifyAsync(Token(scenario), CancellationToken.None);
        Assert.True(result.IsFailed);
        Assert.Equal("Google.InvalidToken", result.Errors.Single().Metadata["Code"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public async Task Verify_MalformedToken_ReturnsFailure(string token)
    {
        Assert.True((await Verifier().VerifyAsync(token, CancellationToken.None)).IsFailed);
    }

    [Fact]
    public async Task Verify_UnconfiguredClient_ReturnsUnavailable()
    {
        var result = await Verifier("").VerifyAsync(Token(), CancellationToken.None);
        Assert.Equal("Google.Unavailable", result.Errors.Single().Metadata["Code"]);
    }

    [Fact]
    public async Task Verify_CancelledRequest_Throws()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Verifier().VerifyAsync(Token(), new CancellationToken(true)));
    }

    private GoogleTokenVerifier Verifier(string clientId = ClientId)
    {
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new RsaSecurityKey(_rsa) { KeyId = "test-key" });
        return new GoogleTokenVerifier(new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            Options.Create(new GoogleAuthOptions { ClientId = clientId }), NullLogger<GoogleTokenVerifier>.Instance);
    }

    private string Token(string scenario = "valid")
    {
        using var otherKey = RSA.Create(2048);
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = scenario == "subject" ? "" : "google-subject",
            ["email"] = scenario == "email" ? "" : "member@example.com",
            ["email_verified"] = scenario != "unverified",
        };
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = scenario == "issuer" ? "https://attacker.example" : "https://accounts.google.com",
            Audience = scenario == "audience" ? "another-client" : ClientId,
            NotBefore = now.AddMinutes(-10),
            IssuedAt = now.AddMinutes(-10),
            Expires = scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(10),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(scenario == "signature" ? otherKey : _rsa) { KeyId = "test-key" },
                SecurityAlgorithms.RsaSha256),
        });
    }

    public void Dispose() => _rsa.Dispose();
}
