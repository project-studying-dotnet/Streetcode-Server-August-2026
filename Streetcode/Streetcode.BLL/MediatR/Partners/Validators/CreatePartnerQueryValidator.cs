using FluentValidation;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.MediatR.Partners.Create;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class CreatePartnerQueryValidator
    : AbstractValidator<CreatePartnerQuery>
{
    public CreatePartnerQueryValidator(
        IValidator<CreatePartnerDTO> partnerValidator)
    {
        RuleFor(query => query.newPartner)
            .NotNull()
            .WithMessage("Partner is required.")
            .SetValidator(partnerValidator);
    }
}