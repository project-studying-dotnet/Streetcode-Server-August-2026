using FluentValidation;
using Streetcode.BLL.MediatR.Newss.GetById;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsByIdQueryValidator
    : AbstractValidator<GetNewsByIdQuery>
{
    public GetNewsByIdQueryValidator()
    {
        RuleFor(query => query.id)
            .GreaterThan(0)
            .WithMessage("News ID must be greater than 0.");
    }
}