using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;
using RelatedTermEntity =
    Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class DeleteRelatedTermCommandValidator
    : AbstractValidator<DeleteRelatedTermCommand>
{
    public DeleteRelatedTermCommandValidator()
    {
        RuleFor(command => command.word)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                RelatedTermEntity.WordMaxLength,
                "Related term word");
    }
}