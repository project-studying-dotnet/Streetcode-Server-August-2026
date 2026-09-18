using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Validators;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(comment => comment.Text)
            .NotEmpty()
            .WithMessage("Comment text is required.")
            .MustNotExceedLength(CommentEntity.TextMaxLength, "Comment text");
    }
}
