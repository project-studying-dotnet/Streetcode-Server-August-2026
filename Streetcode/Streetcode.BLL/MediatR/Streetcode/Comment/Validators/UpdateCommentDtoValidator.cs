using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Validators;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class UpdateCommentDtoValidator
    : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(comment => comment.Text)
            .NotEmpty()
            .WithMessage("Comment text is required.")
            .MustNotExceedLength(CommentEntity.TextMaxLength, "Comment text");

        RuleFor(comment => comment.RowVersion)
            .NotEmpty()
            .WithMessage("Comment version is required.");
    }
}
