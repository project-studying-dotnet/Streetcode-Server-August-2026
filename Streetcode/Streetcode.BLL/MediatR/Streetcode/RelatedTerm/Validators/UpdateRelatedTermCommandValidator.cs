using FluentValidation;
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
            .GreaterThan(0)
            .WithMessage("Related term ID must be greater than 0.");

        RuleFor(command => command.RelatedTerm)
            .NotNull()
            .WithMessage("Related term cannot be null.")
            .SetValidator(termValidator);
    }
}