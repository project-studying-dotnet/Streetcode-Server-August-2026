using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Email.Validators;

public sealed class EmailDtoValidator
    : AbstractValidator<EmailDTO>
{
    private const int FromMaxLength = 80;
    private const int ContentMaxLength = 500;

    public EmailDtoValidator()
    {
        RuleFor(email => email.MessageId)
            .Must(messageId =>
                messageId is null || messageId != Guid.Empty)
            .WithMessage("Message ID must not be empty when provided.");

        RuleFor(email => email.From)
            .NotEmpty()
            .WithName("Sender email")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                FromMaxLength,
                "Sender email")
            .EmailAddress()
            .WithMessage(
                ErrorMessages.SenderMustBeValidEmail);

        RuleFor(email => email.Content)
            .NotEmpty()
            .WithName("Email content")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                ContentMaxLength,
                "Email content");
    }
}