using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Partners.Create;
using Streetcode.DAL.Entities.Partners;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class CreatePartnerSourceLinkDtoValidator
    : AbstractValidator<CreatePartnerSourceLinkDTO>
{
    public CreatePartnerSourceLinkDtoValidator()
    {
        RuleFor(link => link.TargetUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                PartnerSourceLink.TargetUrlMaxLength,
                "Partner source URL")
            .MustBeValidHttpUrl("Partner source URL");

        RuleFor(link => link.LogoType)
            .IsInEnum()
            .WithMessage(ErrorMessages.Invalid_Property);
    }
}