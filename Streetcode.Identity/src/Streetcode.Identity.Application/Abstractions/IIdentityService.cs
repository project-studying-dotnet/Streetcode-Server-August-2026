using FluentResults;

namespace Streetcode.Identity.Application.Abstractions;

public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        DateTime? birthDate,
        string? phone,
        string? gender,
        CancellationToken cancellationToken);
}