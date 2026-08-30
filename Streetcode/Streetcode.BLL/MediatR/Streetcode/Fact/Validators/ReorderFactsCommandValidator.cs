using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class ReorderFactsCommandValidator
    : AbstractValidator<ReorderFactsCommand>
{
    public ReorderFactsCommandValidator(
        IValidator<FactReorderDto> reorderValidator)
    {
        RuleFor(command => command.Reorder)
            .NotNull()
            .WithMessage("Fact reorder data is required.")
            .SetValidator(reorderValidator);
    }
}