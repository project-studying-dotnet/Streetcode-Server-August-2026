using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Comment");
    }
}
