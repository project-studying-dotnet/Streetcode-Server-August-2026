using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.MediatR.Partners.Update;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class UpdatePartnerQueryValidator
    : AbstractValidator<UpdatePartnerQuery>
{
    public UpdatePartnerQueryValidator(
        IValidator<CreatePartnerDTO> partnerValidator)
    {
        RuleFor(query => query.Partner)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(partnerValidator);

        RuleFor(query => query.Partner.Id)
            .MustBeValidId("Partner")
            .When(query => query.Partner is not null);
    }
}