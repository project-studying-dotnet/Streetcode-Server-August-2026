using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class DeleteRelatedTermCommandValidator
    : AbstractValidator<DeleteRelatedTermCommand>
{
    public DeleteRelatedTermCommandValidator()
    {
        RuleFor(command => command.word)
            .NotEmpty()
            .WithMessage("Related term word is required.")
            .MaximumLength(50)
            .WithMessage("Related term word must not exceed 50 characters.");
    }
}