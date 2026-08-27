using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Update;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class UpdateRelatedTermCommandValidator
    : AbstractValidator<UpdateRelatedTermCommand>
{
    public UpdateRelatedTermCommandValidator(
        IValidator<RelatedTermDTO> termValidator)
    {
        RuleFor(command => command.id)
            .MustBeValidId("Related term");

        RuleFor(command => command.RelatedTerm)
            .NotNull()
            .WithMessage("Related term is required.")
            .SetValidator(termValidator);
    }
}