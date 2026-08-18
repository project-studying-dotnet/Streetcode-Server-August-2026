using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetAllCatalog;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesCatalogQueryValidator
    : AbstractValidator<GetAllStreetcodesCatalogQuery>
{
    public GetAllStreetcodesCatalogQueryValidator()
    {
        RuleFor(query => query.page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(query => query.count)
            .GreaterThan(0)
            .WithMessage("Count must be greater than 0.");
    }
}