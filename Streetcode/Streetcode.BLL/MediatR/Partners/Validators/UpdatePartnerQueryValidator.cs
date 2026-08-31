using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.MediatR.Partners.Update;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class UpdatePartnerQueryValidator
    : AbstractValidator<UpdatePartnerQuery>
{
    public UpdatePartnerQueryValidator(
        IValidator<CreatePartnerDTO> partnerValidator)
    {
        RuleFor(query => query.Partner)
            .NotNull()
            .WithMessage("Partner is required.")
            .SetValidator(partnerValidator);

        RuleFor(query => query.Partner.Id)
            .MustBeValidId("Partner")
            .When(query => query.Partner is not null);
    }
}