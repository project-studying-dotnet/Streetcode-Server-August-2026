using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class GetCommentsToReviewRequestDtoValidator
    : AbstractValidator<GetCommentsToReviewRequestDto>
{
    public GetCommentsToReviewRequestDtoValidator()
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
    }
}
