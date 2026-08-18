using FluentValidation;
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
            .WithMessage("Partner cannot be null.")
            .SetValidator(partnerValidator);

        RuleFor(query => query.Partner.Id)
            .GreaterThan(0)
            .WithMessage("Partner ID must be greater than 0.")
            .When(query => query.Partner is not null);
    }
}