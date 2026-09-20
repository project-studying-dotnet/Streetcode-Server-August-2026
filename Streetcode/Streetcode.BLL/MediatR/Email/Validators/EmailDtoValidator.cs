using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Email;

namespace Streetcode.BLL.MediatR.Email.Validators;

public sealed class EmailDtoValidator
    : AbstractValidator<EmailDTO>
{
    private const int FromMaxLength = 80;
    private const int ContentMaxLength = 500;

    public EmailDtoValidator()
    {
        RuleFor(email => email.From)
            .NotEmpty()
            .WithMessage("Sender email is required.")
            .MustNotExceedLength(
                FromMaxLength,
                "Sender email")
            .EmailAddress()
            .WithMessage(
                "Sender email must be a valid email address.");

        RuleFor(email => email.Content)
            .NotEmpty()
            .WithMessage("Email content is required.")
            .MustNotExceedLength(
                ContentMaxLength,
                "Email content");
    }
}