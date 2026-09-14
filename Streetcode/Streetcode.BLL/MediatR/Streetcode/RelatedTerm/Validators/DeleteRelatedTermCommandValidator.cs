using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;
using RelatedTermEntity =
    Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class DeleteRelatedTermCommandValidator
    : AbstractValidator<DeleteRelatedTermCommand>
{
    public DeleteRelatedTermCommandValidator()
    {
        RuleFor(command => command.word)
            .NotEmpty()
            .WithMessage("Related term word is required.")
            .MustNotExceedLength(
                RelatedTermEntity.WordMaxLength,
                "Related term word");
    }
}