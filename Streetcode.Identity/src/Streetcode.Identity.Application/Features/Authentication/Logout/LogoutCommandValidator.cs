using FluentValidation;

namespace Streetcode.Identity.Application.Features.Authentication.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("RefreshToken.Required")
            .MaximumLength(512)
            .WithErrorCode("RefreshToken.TooLong");
    }
}
