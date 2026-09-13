using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class GetCommentsToReviewQueryValidator
    : AbstractValidator<GetCommentsToReviewQuery>
{
    public GetCommentsToReviewQueryValidator(
        IValidator<GetCommentsToReviewRequestDto> requestValidator)
    {
        RuleFor(query => query.Request)
            .NotNull()
            .WithMessage("Request is required.")
            .SetValidator(requestValidator);
    }
}
