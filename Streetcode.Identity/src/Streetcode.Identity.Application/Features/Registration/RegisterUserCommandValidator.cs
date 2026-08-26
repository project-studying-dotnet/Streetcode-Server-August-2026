using FluentValidation;

namespace Streetcode.Identity.Application.Features.Registration;

public sealed class RegisterUserCommandValidator
    : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("Email.Required")
            .EmailAddress().WithErrorCode("Email.Invalid")
            .MaximumLength(256).WithErrorCode("Email.TooLong");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithErrorCode("Password.Required");

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(20)
            .WithErrorCode("PhoneNumber.Required");
    }
}
