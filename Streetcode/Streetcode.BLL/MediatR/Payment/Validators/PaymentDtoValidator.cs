using FluentValidation;
using Streetcode.BLL.DTO.Payment;

namespace Streetcode.BLL.MediatR.Payment.Validators;

public sealed class PaymentDtoValidator : AbstractValidator<PaymentDTO>
{
    public PaymentDtoValidator()
    {
        RuleFor(payment => payment.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");
        RuleFor(payment => payment.RedirectUrl)
            .Must(BeValidRedirectUrl)
            .When(payment => !string.IsNullOrWhiteSpace(payment.RedirectUrl))
            .WithMessage("Redirect URL must be a valid HTTP or HTTPS URL.");
    }

    public static bool BeValidRedirectUrl(string? redirectUrl)
    {
        return Uri.TryCreate(
            redirectUrl,
            UriKind.Absolute,
            out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}