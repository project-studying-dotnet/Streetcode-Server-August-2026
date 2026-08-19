using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Filter;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByFilter;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByFilterQueryValidator
    : AbstractValidator<GetStreetcodeByFilterQuery>
{
    public GetStreetcodeByFilterQueryValidator(
        IValidator<StreetcodeFilterRequestDTO> filterValidator)
    {
        RuleFor(query => query.Filter)
            .NotNull()
            .WithMessage("Filter is required.")
            .SetValidator(filterValidator);
    }
}