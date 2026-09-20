using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Streetcode.Fact.Create;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class CreateFactCommandValidator
    : AbstractValidator<CreateFactCommand>
{
    public CreateFactCommandValidator(
        IValidator<FactUpdateCreateDto> factValidator)
    {
        RuleFor(command => command.Fact)
            .NotNull()
            .WithMessage("Fact is required.")
            .SetValidator(factValidator);
    }
}
