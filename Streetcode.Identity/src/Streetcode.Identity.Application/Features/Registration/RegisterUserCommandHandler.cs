using FluentResults;
using MediatR;
using Streetcode.Identity.Application.Abstractions;

namespace Streetcode.Identity.Application.Features.Registration;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var creationResult = await _identityService.CreateUserAsync(request.Email, request.Password, request.PhoneNumber, cancellationToken);
        if (creationResult.IsFailed)
        {
            return Result.Fail<RegisterUserResponse>(creationResult.Errors);
        }

        var userResponse = new RegisterUserResponse(creationResult.Value, request.Email);
        return Result.Ok(userResponse);
    }
}
