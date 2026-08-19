using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Text.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetTextByIdQueryValidator
    : AbstractValidator<GetTextByIdQuery>
{
    public GetTextByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Text");
    }
}