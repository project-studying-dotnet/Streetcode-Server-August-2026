using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Term.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Term.Validators;

public sealed class GetTermByIdQueryValidator
    : AbstractValidator<GetTermByIdQuery>
{
    public GetTermByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Term");
    }
}