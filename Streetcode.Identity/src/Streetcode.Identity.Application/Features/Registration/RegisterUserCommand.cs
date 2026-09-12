using FluentResults;
using MediatR;

namespace Streetcode.Identity.Application.Features.Registration;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? PhoneNumber)
    : IRequest<Result<RegisterUserResponse>>;