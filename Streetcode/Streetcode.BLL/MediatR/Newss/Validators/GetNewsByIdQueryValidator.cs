using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Newss.GetById;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsByIdQueryValidator
    : AbstractValidator<GetNewsByIdQuery>
{
    public GetNewsByIdQueryValidator()
    {
        RuleFor(query => query.id)
            .MustBeValidId("News");
    }
}