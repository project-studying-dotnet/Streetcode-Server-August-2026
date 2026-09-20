using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Filter;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class StreetcodeFilterRequestDtoValidator
    : AbstractValidator<StreetcodeFilterRequestDTO>
{
    public StreetcodeFilterRequestDtoValidator()
    {
        RuleFor(dto => dto.SearchQuery)
            .NotEmpty()
            .WithName("Search query")
            .WithMessage(ErrorMessages.Field_Required);
    }
}