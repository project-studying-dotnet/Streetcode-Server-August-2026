using FluentValidation;

namespace Streetcode.Identity.Application.Features.Authentication.Login;

public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Email.Required")
            .EmailAddress()
            .WithErrorCode("Email.Invalid")
            .MaximumLength(256)
            .WithErrorCode("Email.TooLong");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Password.Required")
            .MaximumLength(256)
            .WithErrorCode("Password.TooLong");
    }
}
