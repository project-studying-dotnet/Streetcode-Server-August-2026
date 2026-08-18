using FluentValidation;
using Streetcode.BLL.MediatR.Newss.Delete;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class DeleteNewsCommandValidator
    : AbstractValidator<DeleteNewsCommand>
{
    public DeleteNewsCommandValidator()
    {
        RuleFor(command => command.id)
            .GreaterThan(0)
            .WithMessage("News ID must be greater than 0.");
    }
}