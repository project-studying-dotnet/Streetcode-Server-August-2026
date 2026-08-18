using FluentValidation;
using Streetcode.BLL.MediatR.Sources.SourceLink.GetCategoryById;

namespace Streetcode.BLL.MediatR.Sources.SourceLinkCategory.Validators;

public sealed class GetCategoryByIdQueryValidator
    : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Source category ID must be greater than 0.");
    }
}