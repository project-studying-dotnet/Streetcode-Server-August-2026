using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;
using Streetcode.BLL.Resources;
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
            .WithName("Related term word")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                RelatedTermEntity.WordMaxLength,
                "Related term word");

        RuleFor(command => command.termId)
            .GreaterThan(0)
            .WithMessage("Term id must be greater than zero.");
    }
}
