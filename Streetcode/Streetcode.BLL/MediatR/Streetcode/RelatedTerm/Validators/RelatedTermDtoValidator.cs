using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Resources;
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
            .WithName("Related term word")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                RelatedTermEntity.WordMaxLength,
                "Related term word");

        RuleFor(term => term.TermId)
            .MustBeValidId("Term");
    }
}