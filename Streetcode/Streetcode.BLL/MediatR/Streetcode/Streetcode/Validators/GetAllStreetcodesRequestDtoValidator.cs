using FluentValidation;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesRequestDtoValidator
    : AbstractValidator<GetAllStreetcodesRequestDTO>
{
    public GetAllStreetcodesRequestDtoValidator()
    {
        RuleFor(dto => dto.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(dto => dto.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(dto => dto.Title)
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters.")
            .When(dto => dto.Title is not null);

        RuleFor(dto => dto.Sort)
            .Must(BeValidSortProperty)
            .WithMessage("Sort must contain a valid Streetcode property.")
            .When(dto => dto.Sort is not null);

        RuleFor(dto => dto.Filter)
            .Must(HaveValidFilterFormat)
            .WithMessage("Filter must have the format 'name:value'.")
            .When(dto => dto.Filter is not null);
    }

    private static bool BeValidSortProperty(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return false;
        }

        string propertyName = sort.Trim().TrimStart('-');

        return typeof(StreetcodeContent).GetProperty(propertyName) is not null;
    }

    private static bool HaveValidFilterFormat(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return false;
        }

        int separatorIndex = filter.IndexOf(':');

        return separatorIndex > 0 && separatorIndex < filter.Length - 1;
    }
}