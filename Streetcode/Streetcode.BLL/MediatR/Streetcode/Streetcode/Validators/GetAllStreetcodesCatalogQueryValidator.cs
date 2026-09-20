using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetAllCatalog;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesCatalogQueryValidator
    : AbstractValidator<GetAllStreetcodesCatalogQuery>
{
    public GetAllStreetcodesCatalogQueryValidator()
    {
        RuleFor(query => query.page)
            .GreaterThan(0)
            .WithName("Page")
            .WithMessage(ErrorMessages.Field_Required);

        RuleFor(query => query.count)
            .GreaterThan(0)
            .WithName("Count")
            .WithMessage(ErrorMessages.Field_Required)
            .LessThanOrEqualTo(PaginationLimits.MaxPageSize)
            .WithMessage(
                $"Count must not exceed {PaginationLimits.MaxPageSize}.");
    }
}