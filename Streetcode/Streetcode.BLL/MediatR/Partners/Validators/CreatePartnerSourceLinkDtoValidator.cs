using FluentValidation;
using Streetcode.BLL.DTO.Partners.Create;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class CreatePartnerSourceLinkDtoValidator
    : AbstractValidator<CreatePartnerSourceLinkDTO>
{
    public CreatePartnerSourceLinkDtoValidator()
    {
        RuleFor(link => link.TargetUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Partner source URL is required.")
            .MaximumLength(255)
            .WithMessage("Partner source URL must not exceed 255 characters.")
            .Must(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                    || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Partner source URL must be a valid HTTP or HTTPS URL.");

        RuleFor(link => link.LogoType)
            .IsInEnum()
            .WithMessage("Partner source logo type is invalid.");
    }
}