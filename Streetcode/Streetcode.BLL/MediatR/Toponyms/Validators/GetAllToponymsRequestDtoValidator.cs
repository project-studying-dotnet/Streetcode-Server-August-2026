using FluentValidation;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.DAL.Entities.Toponyms;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetAllToponymsRequestDtoValidator
    : AbstractValidator<GetAllToponymsRequestDTO>
{
    public GetAllToponymsRequestDtoValidator()
    {
        RuleFor(dto => dto.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(dto => dto.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(PaginationLimits.MaxPageSize)
            .WithMessage($"Amount must not exceed {PaginationLimits.MaxPageSize}.");

        RuleFor(dto => dto.Title)
            .MustNotExceedLength(
                Toponym.StreetNameMaxLength,
                "Title")
            .When(dto => dto.Title is not null);
    }
}
