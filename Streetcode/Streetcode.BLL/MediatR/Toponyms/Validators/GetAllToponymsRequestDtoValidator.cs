using FluentValidation;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetAllToponymsRequestDtoValidator
    : AbstractValidator<GetAllToponymsRequestDTO>
{
    public GetAllToponymsRequestDtoValidator()
    {
        RuleFor(dto => dto.Page)
            .GreaterThan(0)
            .WithMessage(ErrorMessages.PropertyGreaterThan_Zero);

        RuleFor(dto => dto.Amount)
            .GreaterThan(0)
            .WithMessage(ErrorMessages.PropertyGreaterThan_Zero)
            .LessThanOrEqualTo(PaginationLimits.MaxPageSize)
            .WithMessage(string.Format(ErrorMessages.MustNotExceedPaginationLimits, PaginationLimits.MaxPageSize));

        RuleFor(dto => dto.Title)
            .MustNotExceedLength(
                Toponym.StreetNameMaxLength,
                "Title")
            .When(dto => dto.Title is not null);
    }
}
