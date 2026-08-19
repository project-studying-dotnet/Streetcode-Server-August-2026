using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Partners.Create;
using Streetcode.DAL.Entities.Partners;

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
            .MustNotExceedLength(Partner.TitleMaxLength, "Partner title");

        RuleFor(partner => partner.LogoId)
            .MustBeValidId("Partner logo");

        RuleFor(partner => partner.Description)
            .MustNotExceedLength(
                Partner.DescriptionMaxLength,
                "Partner description");

        RuleFor(partner => partner.UrlTitle)
            .MustNotExceedLength(
                Partner.UrlTitleMaxLength,
                "Partner URL title");

        RuleFor(partner => partner.TargetUrl)
            .Cascade(CascadeMode.Stop)
            .MustNotExceedLength(
                Partner.TargetUrlMaxLength,
                "Partner URL")
            .MustBeValidHttpUrl("Partner URL")
            .When(partner => !string.IsNullOrWhiteSpace(partner.TargetUrl));

        RuleForEach(partner => partner.PartnerSourceLinks)
            .SetValidator(sourceLinkValidator);
    }
}