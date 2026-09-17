using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class GetFactByIdQueryValidator
    : AbstractValidator<GetFactByIdQuery>
{
    public GetFactByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Fact");
    }
}