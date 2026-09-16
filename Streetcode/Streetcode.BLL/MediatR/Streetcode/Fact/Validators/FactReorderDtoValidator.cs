using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class FactReorderDtoValidator
    : AbstractValidator<FactReorderDto>
{
    public FactReorderDtoValidator()
    {
        RuleFor(reorder => reorder.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(reorder => reorder.OrderedFactIds)
            .NotNull()
            .WithMessage("Ordered fact IDs are required.")
            .Must(ids =>
                ids is null ||
                ids.Count == ids.Distinct().Count())
            .WithMessage("Fact order contains duplicate IDs.");

        RuleForEach(reorder => reorder.OrderedFactIds)
            .MustBeValidId("Fact");
    }
}
