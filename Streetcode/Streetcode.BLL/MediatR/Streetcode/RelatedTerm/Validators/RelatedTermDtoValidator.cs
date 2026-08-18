using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class RelatedTermDtoValidator
    : AbstractValidator<RelatedTermDTO>
{
    public RelatedTermDtoValidator()
    {
        RuleFor(term => term.Word)
            .NotEmpty()
            .WithMessage("Related term word is required.")
            .MaximumLength(50)
            .WithMessage("Related term word must not exceed 50 characters.");

        RuleFor(term => term.TermId)
            .GreaterThan(0)
            .WithMessage("Term ID must be greater than 0.");
    }
}