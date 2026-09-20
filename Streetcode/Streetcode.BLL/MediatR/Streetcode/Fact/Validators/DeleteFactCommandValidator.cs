using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Fact.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class DeleteFactCommandValidator
    : AbstractValidator<DeleteFactCommand>
{
    public DeleteFactCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Fact");
    }
}