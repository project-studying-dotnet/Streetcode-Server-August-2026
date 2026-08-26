using FluentResults;
using MediatR;

namespace Streetcode.Identity.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(string RefreshToken)
    : IRequest<Result>;
