using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByIdQueryValidator
    : AbstractValidator<GetStreetcodeByIdQuery>
{
    public GetStreetcodeByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}