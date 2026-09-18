namespace Streetcode.Identity.Application.Features.Authentication.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
