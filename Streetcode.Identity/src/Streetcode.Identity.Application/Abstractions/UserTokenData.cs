namespace Streetcode.Identity.Application.Abstractions;

public record UserTokenData(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    long AccessVersion,
    bool IsActive);
