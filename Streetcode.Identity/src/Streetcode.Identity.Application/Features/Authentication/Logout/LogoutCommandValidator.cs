using FluentValidation;

namespace Streetcode.Identity.Application.Features.Authentication.Logout;

public sealed class LogoutCommandValidator
    : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshToken.Required");
    }
}
