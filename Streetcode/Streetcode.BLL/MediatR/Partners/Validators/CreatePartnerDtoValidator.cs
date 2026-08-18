using FluentValidation;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Partners.Create;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class CreatePartnerDtoValidator
    : AbstractValidator<CreatePartnerDTO>
{
    public CreatePartnerDtoValidator(
        IValidator<CreatePartnerSourceLinkDTO> sourceLinkValidator)
    {
        RuleFor(partner => partner.Title)
            .NotEmpty()
            .WithMessage("Partner title is required.")
            .MaximumLength(255)
            .WithMessage("Partner title must not exceed 255 characters.");

        RuleFor(partner => partner.LogoId)
            .GreaterThan(0)
            .WithMessage("Partner logo ID must be greater than 0.");

        RuleFor(partner => partner.Description)
            .MaximumLength(600)
            .WithMessage("Partner description must not exceed 600 characters.");

        RuleFor(partner => partner.UrlTitle)
            .MaximumLength(255)
            .WithMessage("Partner URL title must not exceed 255 characters.");

        RuleFor(partner => partner.TargetUrl)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(255)
            .WithMessage("Partner URL must not exceed 255 characters.")
            .Must(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                    || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Partner URL must be a valid HTTP or HTTPS URL.")
            .When(partner => !string.IsNullOrWhiteSpace(partner.TargetUrl));

        RuleForEach(partner => partner.PartnerSourceLinks)
            .SetValidator(sourceLinkValidator);
    }
}