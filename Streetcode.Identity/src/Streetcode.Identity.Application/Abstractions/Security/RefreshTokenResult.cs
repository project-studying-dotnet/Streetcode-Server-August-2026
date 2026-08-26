namespace Streetcode.Identity.Application.Abstractions.Security;

public sealed record RefreshTokenResult(
    Guid UserId,
    string Token,
    DateTimeOffset ExpiresAt);
