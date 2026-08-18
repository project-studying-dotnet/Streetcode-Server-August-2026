using FluentValidation;
using Streetcode.BLL.DTO.Payment;
using Streetcode.BLL.MediatR.Payment;

namespace Streetcode.Bll.MediaR.Payment.Validators;

public sealed class CreateInvoiceCommandValidator
    : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator(
        IValidator<PaymentDTO> paymentDtoValidator)
    {
        RuleFor(command => command.Payment)
            .NotNull()
            .WithMessage("Payment data is required.")
            .SetValidator(paymentDtoValidator);
    }
}