namespace Streetcode.Identity.Application.Features.Registration;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email);