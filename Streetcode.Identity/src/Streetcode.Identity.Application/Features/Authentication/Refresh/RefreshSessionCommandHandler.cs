using FluentResults;
using MediatR;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Application.Features.Authentication.Refresh;

public sealed class RefreshSessionCommandHandler
    : IRequestHandler<
        RefreshSessionCommand,
        Result<RefreshSessionResponse>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;

    public RefreshSessionCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIdentityService identityService,
        IJwtService jwtService)
    {
        _refreshTokenService = refreshTokenService;
        _identityService = identityService;
        _jwtService = jwtService;
    }

    public async Task<Result<RefreshSessionResponse>> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken)
    {
        var rotationResult = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);

        if (rotationResult.IsFailed)
        {
            return Result.Fail<RefreshSessionResponse>(
                rotationResult.Errors);
        }

        var userDataResult = await _identityService.GetUserTokenDataAsync(
            rotationResult.Value.UserId,
            cancellationToken);

        if (userDataResult.IsFailed || !userDataResult.Value.IsActive)
        {
            await _refreshTokenService.RevokeFamilyAsync(
                rotationResult.Value.Token,
                cancellationToken);

            return Result.Fail<RefreshSessionResponse>(
                new Error("The refresh session is invalid")
                    .WithMetadata("Code", "RefreshToken.Invalid"));
        }

        var userData = userDataResult.Value;

        var accessToken = _jwtService.GenerateToken(
            userData.UserId,
            userData.Email,
            userData.Roles,
            userData.AccessVersion);

        var response = new RefreshSessionResponse(
            accessToken.Token,
            new DateTimeOffset(accessToken.Expiration),
            rotationResult.Value.Token,
            rotationResult.Value.ExpiresAt);

        return Result.Ok(response);
    }
}
