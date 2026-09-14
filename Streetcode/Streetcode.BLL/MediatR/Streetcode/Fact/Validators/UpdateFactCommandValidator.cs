using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Streetcode.Fact.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class UpdateFactCommandValidator
    : AbstractValidator<UpdateFactCommand>
{
    public UpdateFactCommandValidator(
        IValidator<FactUpdateCreateDto> factValidator)
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Fact");

        RuleFor(command => command.Fact)
            .NotNull()
            .WithMessage("Fact is required.")
            .SetValidator(factValidator);
    }
}