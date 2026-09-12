using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using RelatedTermEntity =
    Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class RelatedTermDtoValidator
    : AbstractValidator<RelatedTermDTO>
{
    public RelatedTermDtoValidator()
    {
        RuleFor(term => term.Word)
            .NotEmpty()
            .WithMessage("Related term word is required.")
            .MustNotExceedLength(
                RelatedTermEntity.WordMaxLength,
                "Related term word");

        RuleFor(term => term.TermId)
            .MustBeValidId("Term");
    }
}