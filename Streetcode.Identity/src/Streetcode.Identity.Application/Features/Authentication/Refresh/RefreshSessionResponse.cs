namespace Streetcode.Identity.Application.Features.Authentication.Refresh;

public sealed record RefreshSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
