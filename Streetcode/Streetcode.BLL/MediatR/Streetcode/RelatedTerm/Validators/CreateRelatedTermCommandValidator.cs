using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Create;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class CreateRelatedTermCommandValidator
    : AbstractValidator<CreateRelatedTermCommand>
{
    public CreateRelatedTermCommandValidator(
        IValidator<RelatedTermDTO> termValidator)
    {
        RuleFor(command => command.RelatedTerm)
            .NotNull()
            .WithMessage("Related term is required.")
            .SetValidator(termValidator);
    }
}