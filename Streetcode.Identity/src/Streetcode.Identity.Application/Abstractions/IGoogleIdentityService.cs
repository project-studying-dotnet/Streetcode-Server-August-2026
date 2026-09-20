using FluentResults;

namespace Streetcode.Identity.Application.Abstractions;

public sealed record GoogleAccount(string Subject, string Email);

public interface IGoogleTokenVerifier
{
    Task<Result<GoogleAccount>> VerifyAsync(string idToken, CancellationToken cancellationToken);
}

public interface IGoogleIdentityService
{
    Task<Result<UserTokenData>> AuthenticateAsync(GoogleAccount account, CancellationToken cancellationToken);

    Task<Result> LinkAsync(GoogleAccount account, Guid userId, long accessVersion,
        string password, CancellationToken cancellationToken);
}
