using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodesByIdsQueryValidator
    : AbstractValidator<GetStreetcodesByIdsQuery>
{
    public GetStreetcodesByIdsQueryValidator()
    {
        RuleFor(query => query.Ids)
            .Must(ids => ids.Count <= PaginationLimits.MaxPageSize)
            .WithMessage(
                $"Ids must not contain more than {PaginationLimits.MaxPageSize} items.");

        RuleForEach(query => query.Ids)
            .MustBeValidId("Streetcode");
    }
}
