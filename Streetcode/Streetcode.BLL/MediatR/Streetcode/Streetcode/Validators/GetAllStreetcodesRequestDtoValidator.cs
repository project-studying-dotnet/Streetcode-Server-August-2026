using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Filters;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesRequestDtoValidator
    : AbstractValidator<GetAllStreetcodesRequestDTO>
{
    private static readonly HashSet<string> AllowedSortProperties =
        new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(StreetcodeContent.Id),
        nameof(StreetcodeContent.Index),
        nameof(StreetcodeContent.Teaser),
        nameof(StreetcodeContent.DateString),
        nameof(StreetcodeContent.Alias),
        nameof(StreetcodeContent.Status),
        nameof(StreetcodeContent.Title),
        nameof(StreetcodeContent.TransliterationUrl),
        nameof(StreetcodeContent.ViewCount),
        nameof(StreetcodeContent.CreatedAt),
        nameof(StreetcodeContent.UpdatedAt),
        nameof(StreetcodeContent.EventStartOrPersonBirthDate),
        nameof(StreetcodeContent.EventEndOrPersonDeathDate),
        nameof(StreetcodeContent.AudioId),
    };

    public GetAllStreetcodesRequestDtoValidator()
    {
        RuleFor(dto => dto.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(dto => dto.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(PaginationLimits.MaxPageSize)
            .WithMessage(
                $"Amount must not exceed {PaginationLimits.MaxPageSize}.");

        RuleFor(dto => dto.Title)
            .MustNotExceedLength(
                StreetcodeContent.TitleMaxLength,
                "Title")
            .When(dto => dto.Title is not null);

        RuleFor(dto => dto.Sort)
            .Must(BeValidSortProperty)
            .WithMessage("Sort must contain a valid sortable Streetcode property.")
            .When(dto => dto.Sort is not null);

        RuleFor(dto => dto.Filter)
            .Must(filter =>
                StreetcodeFilterParser.TryParse(filter, out _))
            .WithMessage(
                "Filter must have the format 'status:<Draft|Published|Deleted>'.")
            .When(dto => dto.Filter is not null);
    }

    private static bool BeValidSortProperty(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return false;
        }

        string propertyName = sort.Trim();

        if (propertyName.StartsWith('-'))
        {
            propertyName = propertyName[1..];
        }

        return AllowedSortProperties.Contains(propertyName);
    }
}
