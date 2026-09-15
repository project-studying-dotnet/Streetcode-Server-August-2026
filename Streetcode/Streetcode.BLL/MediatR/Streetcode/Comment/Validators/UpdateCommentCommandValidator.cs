using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class UpdateCommentCommandValidator
    : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator(
        IValidator<UpdateCommentDto> commentValidator)
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Comment");

        RuleFor(command => command.Comment)
            .NotNull()
            .WithMessage("Comment is required.")
            .SetValidator(commentValidator);
    }
}
