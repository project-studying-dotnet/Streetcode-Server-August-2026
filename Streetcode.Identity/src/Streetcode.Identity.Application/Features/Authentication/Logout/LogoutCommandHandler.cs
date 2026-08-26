using FluentResults;
using MediatR;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Application.Features.Authentication.Logout;

public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public Task<Result> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        return _refreshTokenService.RevokeFamilyAsync(
            request.RefreshToken,
            cancellationToken);
    }
}
