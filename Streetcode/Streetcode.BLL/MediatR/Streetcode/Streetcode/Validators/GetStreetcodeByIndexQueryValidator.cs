using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIndex;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByIndexQueryValidator
    : AbstractValidator<GetStreetcodeByIndexQuery>
{
    public GetStreetcodeByIndexQueryValidator()
    {
        RuleFor(query => query.Index)
            .GreaterThan(0)
            .WithMessage("Streetcode index must be greater than 0.");
    }
}