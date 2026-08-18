using FluentValidation;
using Streetcode.BLL.DTO.Email;

namespace Streetcode.BLL.MediatR.Email.Validators;

public sealed class EmailDtoValidator
    : AbstractValidator<EmailDTO>
{
    public EmailDtoValidator()
    {
        RuleFor(email => email.From)
            .NotEmpty()
            .WithMessage("Sender email is required.")
            .MaximumLength(80)
            .WithMessage(
                "Sender email must not exceed 80 characters.")
            .EmailAddress()
            .WithMessage(
                "Sender email must be a valid email address.");

        RuleFor(email => email.Content)
            .NotEmpty()
            .WithMessage("Email content is required.")
            .MaximumLength(500)
            .WithMessage(
                "Email content must not exceed 500 characters.");
    }
}