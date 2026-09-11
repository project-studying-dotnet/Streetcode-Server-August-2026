using FluentValidation;
using Streetcode.BLL.DTO.Payment;
using Streetcode.BLL.MediatR.Payment;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Payment.Validators;

public sealed class CreateInvoiceCommandValidator
    : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator(
        IValidator<PaymentDTO> paymentDtoValidator)
    {
        RuleFor(command => command.Payment)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(paymentDtoValidator);
    }
}