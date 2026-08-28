using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetCategoryContentByStreetcodeId;

namespace Streetcode.BLL.MediatR.Sources.SourceLinkCategory.Validators;

public sealed class GetCategoryContentByStreetcodeIdQueryValidator
    : AbstractValidator<GetCategoryContentByStreetcodeIdQuery>
{
    public GetCategoryContentByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.streetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(query => query.categoryId)
            .MustBeValidId("Source category");
    }
}