using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Partners.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class GetPartnersByStreetcodeIdQueryValidator
    : AbstractValidator<GetPartnersByStreetcodeIdQuery>
{
    public GetPartnersByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}