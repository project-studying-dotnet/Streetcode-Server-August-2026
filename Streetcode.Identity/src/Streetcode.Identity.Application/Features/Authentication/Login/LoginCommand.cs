using FluentResults;
using MediatR;

namespace Streetcode.Identity.Application.Features.Authentication.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<Result<LoginResponse>>;
