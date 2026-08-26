using FluentResults;
using MediatR;

namespace Streetcode.Identity.Application.Features.Registration;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    DateTime? BirthDate,
    string? Phone,
    string? Gender)
    : IRequest<Result<RegisterUserResponse>>;