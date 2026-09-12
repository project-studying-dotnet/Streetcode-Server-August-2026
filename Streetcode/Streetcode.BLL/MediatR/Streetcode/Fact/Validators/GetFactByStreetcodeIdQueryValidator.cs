using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class GetFactByStreetcodeIdQueryValidator
    : AbstractValidator<GetFactByStreetcodeIdQuery>
{
    public GetFactByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}