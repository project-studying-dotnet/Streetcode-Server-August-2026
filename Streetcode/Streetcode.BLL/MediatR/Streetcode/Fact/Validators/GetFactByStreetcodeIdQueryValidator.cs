using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class GetFactByStreetcodeIdQueryValidator
    : AbstractValidator<GetFactByStreetcodeIdQuery>
{
    public GetFactByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}