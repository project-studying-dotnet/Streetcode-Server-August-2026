using FluentResults;
using MediatR;

namespace Streetcode.Identity.Application.Features.Authentication.Refresh;

public sealed record RefreshSessionCommand(string RefreshToken)
    : IRequest<Result<RefreshSessionResponse>>;
