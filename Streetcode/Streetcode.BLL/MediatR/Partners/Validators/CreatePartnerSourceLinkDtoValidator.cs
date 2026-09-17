using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Partners.Create;
using Streetcode.DAL.Entities.Partners;

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
            .MustNotExceedLength(
                PartnerSourceLink.TargetUrlMaxLength,
                "Partner source URL")
            .MustBeValidHttpUrl("Partner source URL");

        RuleFor(link => link.LogoType)
            .IsInEnum()
            .WithMessage("Partner source logo type is invalid.");
    }
}