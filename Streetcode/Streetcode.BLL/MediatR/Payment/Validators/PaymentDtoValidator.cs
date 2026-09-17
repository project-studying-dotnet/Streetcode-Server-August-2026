using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Payment;

namespace Streetcode.BLL.MediatR.Payment.Validators;

public sealed class PaymentDtoValidator : AbstractValidator<PaymentDTO>
{
    public PaymentDtoValidator()
    {
        RuleFor(payment => payment.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than 0.");
        RuleFor(payment => payment.RedirectUrl)
            .MustBeValidHttpUrl("Redirect URL")
            .When(payment => !string.IsNullOrWhiteSpace(payment.RedirectUrl));
    }
}