using FluentValidation;
using Streetcode.BLL.DTO.Email;

namespace Streetcode.BLL.MediatR.Email.Validators;

public sealed class SendEmailCommandValidator
    : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator(
        IValidator<EmailDTO> emailDtoValidator)
    {
        RuleFor(command => command.Email)
            .NotNull()
            .WithMessage("Email is required.")
            .SetValidator(emailDtoValidator);
    }
}