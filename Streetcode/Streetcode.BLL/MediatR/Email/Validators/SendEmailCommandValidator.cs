using FluentValidation;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Email.Validators;

public sealed class SendEmailCommandValidator
    : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator(
        IValidator<EmailDTO> emailDtoValidator)
    {
        RuleFor(command => command.Email)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(emailDtoValidator);
    }
}