using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Payment;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Payment.Validators;

public sealed class PaymentDtoValidator : AbstractValidator<PaymentDTO>
{
    public PaymentDtoValidator()
    {
        RuleFor(payment => payment.Amount)
            .GreaterThan(0)
            .WithMessage(ErrorMessages.PaymentGreaterThan_Zero);
        RuleFor(payment => payment.RedirectUrl)
            .MustBeValidHttpUrl("Redirect URL")
            .When(payment => !string.IsNullOrWhiteSpace(payment.RedirectUrl));
    }
}