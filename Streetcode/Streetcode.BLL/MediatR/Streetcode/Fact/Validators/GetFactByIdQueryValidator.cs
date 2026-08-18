using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class GetFactByIdQueryValidator
    : AbstractValidator<GetFactByIdQuery>
{
    public GetFactByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Fact ID must be greater than 0.");
    }
}