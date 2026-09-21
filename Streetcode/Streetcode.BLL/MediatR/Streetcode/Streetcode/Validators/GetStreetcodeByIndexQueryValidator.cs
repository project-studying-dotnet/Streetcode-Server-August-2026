using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIndex;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByIndexQueryValidator
    : AbstractValidator<GetStreetcodeByIndexQuery>
{
    public GetStreetcodeByIndexQueryValidator()
    {
        RuleFor(query => query.Index)
            .GreaterThan(0)
            .WithName("Streetcode Index")
            .WithMessage(ErrorMessages.PropertyGreaterThan_Zero);
    }
}