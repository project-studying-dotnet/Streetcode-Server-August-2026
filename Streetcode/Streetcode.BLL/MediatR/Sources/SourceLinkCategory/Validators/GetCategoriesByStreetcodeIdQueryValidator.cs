using FluentValidation;
using Streetcode.BLL.MediatR.Sources.SourceLink.GetCategoriesByStreetcodeId;

namespace Streetcode.BLL.MediatR.Sources.SourceLinkCategory.Validators;

public sealed class GetCategoriesByStreetcodeIdQueryValidator
    : AbstractValidator<GetCategoriesByStreetcodeIdQuery>
{
    public GetCategoriesByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}