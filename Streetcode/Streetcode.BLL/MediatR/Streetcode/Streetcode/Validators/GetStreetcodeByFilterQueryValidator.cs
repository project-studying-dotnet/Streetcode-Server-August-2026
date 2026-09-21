using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Filter;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByFilter;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByFilterQueryValidator
    : AbstractValidator<GetStreetcodeByFilterQuery>
{
    public GetStreetcodeByFilterQueryValidator(
        IValidator<StreetcodeFilterRequestDTO> filterValidator)
    {
        RuleFor(query => query.Filter)
            .NotNull()
            .WithName("Filter")
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(filterValidator);
    }
}