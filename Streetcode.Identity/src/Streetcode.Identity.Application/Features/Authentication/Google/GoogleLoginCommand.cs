using FluentResults;
using FluentValidation;
using MediatR;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Login;

namespace Streetcode.Identity.Application.Features.Authentication.Google;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<Result<LoginResponse>>;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator() => RuleFor(x => x.IdToken).NotEmpty().MaximumLength(16384);
}

public sealed class GoogleLoginCommandHandler(
    IGoogleTokenVerifier verifier,
    IGoogleIdentityService identity,
    IRefreshTokenService refreshTokens,
    IJwtService jwt) : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var verification = await verifier.VerifyAsync(request.IdToken, cancellationToken);
        if (verification.IsFailed) return Result.Fail<LoginResponse>(verification.Errors);

        var authentication = await identity.AuthenticateAsync(verification.Value, cancellationToken);
        if (authentication.IsFailed) return Result.Fail<LoginResponse>(authentication.Errors);

        var user = authentication.Value;
        var refresh = await refreshTokens.IssueAsync(user.UserId, cancellationToken);
        if (refresh.IsFailed) return Result.Fail<LoginResponse>(refresh.Errors);

        var token = jwt.GenerateToken(user.UserId, user.Email, user.Roles, user.AccessVersion);
        return Result.Ok(new LoginResponse(token.Token, new DateTimeOffset(token.Expiration),
            refresh.Value.Token, refresh.Value.ExpiresAt));
    }
}
