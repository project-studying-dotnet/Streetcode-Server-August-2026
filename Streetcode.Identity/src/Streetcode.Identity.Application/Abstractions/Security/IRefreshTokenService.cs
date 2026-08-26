using FluentResults;

namespace Streetcode.Identity.Application.Abstractions.Security;

public interface IRefreshTokenService
{
    Task<Result<RefreshTokenResult>> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result<RefreshTokenResult>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Result> RevokeFamilyAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}