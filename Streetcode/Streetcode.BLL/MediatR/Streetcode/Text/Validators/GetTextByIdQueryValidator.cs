using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Text.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetTextByIdQueryValidator
    : AbstractValidator<GetTextByIdQuery>
{
    public GetTextByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Text ID must be greater than 0.");
    }
}