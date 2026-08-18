using FluentValidation;
using Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetCategoryContentByStreetcodeId;

namespace Streetcode.BLL.MediatR.Sources.SourceLinkCategory.Validators;

public sealed class GetCategoryContentByStreetcodeIdQueryValidator
    : AbstractValidator<GetCategoryContentByStreetcodeIdQuery>
{
    public GetCategoryContentByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.streetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");

        RuleFor(query => query.categoryId)
            .GreaterThan(0)
            .WithMessage("Source category ID must be greater than 0.");
    }
}