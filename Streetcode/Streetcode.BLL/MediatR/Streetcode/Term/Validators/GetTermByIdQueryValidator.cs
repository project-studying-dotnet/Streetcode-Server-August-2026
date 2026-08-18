using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Term.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Term.Validators;

public sealed class GetTermByIdQueryValidator
    : AbstractValidator<GetTermByIdQuery>
{
    public GetTermByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Term ID must be greater than 0.");
    }
}