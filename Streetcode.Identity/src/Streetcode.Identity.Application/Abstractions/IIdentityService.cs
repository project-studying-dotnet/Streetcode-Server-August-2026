using FluentResults;

namespace Streetcode.Identity.Application.Abstractions;

public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken);

    Task<Result<UserTokenData>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result<UserTokenData>> GetUserTokenDataAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
