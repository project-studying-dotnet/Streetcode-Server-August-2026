using FluentResults;
using MediatR;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Application.Features.Authentication.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var authenticationResult =
            await _identityService.AuthenticateAsync(
                request.Email,
                request.Password,
                cancellationToken);

        if (authenticationResult.IsFailed)
        {
            return Result.Fail<LoginResponse>(authenticationResult.Errors);
        }

        var userData = authenticationResult.Value;

        var refreshTokenResult = await _refreshTokenService.IssueAsync(
            userData.UserId,
            cancellationToken);

        if (refreshTokenResult.IsFailed)
        {
            return Result.Fail<LoginResponse>(
                refreshTokenResult.Errors);
        }

        var accessToken = _jwtService.GenerateToken(
            userData.UserId,
            userData.Email,
            userData.Roles,
            userData.AccessVersion);

        var response = new LoginResponse(
            accessToken.Token,
            new DateTimeOffset(accessToken.Expiration),
            refreshTokenResult.Value.Token,
            refreshTokenResult.Value.ExpiresAt);

        return Result.Ok(response);
    }
}
