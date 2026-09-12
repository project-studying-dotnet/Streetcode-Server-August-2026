using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Toponyms.GetById;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetToponymByIdQueryValidator
    : AbstractValidator<GetToponymByIdQuery>
{
    public GetToponymByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Toponym");
    }
}