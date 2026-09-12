using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Filter;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class StreetcodeFilterRequestDtoValidator
    : AbstractValidator<StreetcodeFilterRequestDTO>
{
    public StreetcodeFilterRequestDtoValidator()
    {
        RuleFor(dto => dto.SearchQuery)
            .NotEmpty()
            .WithMessage("Search query is required.");
    }
}