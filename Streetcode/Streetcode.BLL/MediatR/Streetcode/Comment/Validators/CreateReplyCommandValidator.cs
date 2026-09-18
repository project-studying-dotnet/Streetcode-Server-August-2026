using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.Reply;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class CreateReplyCommandValidator : AbstractValidator<CreateReplyCommand>
{
    public CreateReplyCommandValidator(IValidator<CreateCommentDto> replyValidator)
    {
        RuleFor(command => command.ParentCommentId)
            .MustBeValidId("Parent comment");

        RuleFor(command => command.AuthorId)
            .NotEmpty()
            .WithMessage("Author ID is required.");

        RuleFor(command => command.Reply)
            .NotNull()
            .WithMessage("Reply is required.")
            .SetValidator(replyValidator);
    }
}
