using FluentResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Features.Authentication.Google;

namespace Streetcode.Identity.WebApi.Google;

public sealed class GoogleTokenVerifier(
    IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleTokenVerifier> logger) : IGoogleTokenVerifier
{
    public async Task<Result<GoogleAccount>> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(options.Value.ClientId))
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.Unavailable());
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 16384)
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.InvalidToken());

        OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await configurationManager.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning("Google signing configuration is unavailable ({ExceptionType})", exception.GetType().Name);
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.Unavailable());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.Unavailable());
        }

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            ValidateAudience = true,
            ValidAudience = options.Value.ClientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        });
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsValid)
        {
            if (result.Exception is SecurityTokenSignatureKeyNotFoundException) configurationManager.RequestRefresh();
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.InvalidToken());
        }

        var claims = result.ClaimsIdentity;
        var subject = claims.FindFirst("sub")?.Value;
        var email = claims.FindFirst("email")?.Value;
        var verified = claims.FindFirst("email_verified")?.Value;
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255 ||
            string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !bool.TryParse(verified, out var isVerified) || !isVerified)
            return Result.Fail<GoogleAccount>(GoogleAuthErrors.InvalidToken());

        return Result.Ok(new GoogleAccount(subject, email));
    }
}
