using FluentValidation;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.MediatR.Partners.Create;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class CreatePartnerQueryValidator
    : AbstractValidator<CreatePartnerQuery>
{
    public CreatePartnerQueryValidator(
        IValidator<CreatePartnerDTO> partnerValidator)
    {
        RuleFor(query => query.newPartner)
            .NotNull()
            .WithName("Partner")
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(partnerValidator);
    }
}