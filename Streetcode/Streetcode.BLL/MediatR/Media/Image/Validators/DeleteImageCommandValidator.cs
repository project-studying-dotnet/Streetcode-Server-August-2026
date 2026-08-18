using FluentValidation;
using Streetcode.BLL.MediatR.Media.Image.Delete;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class DeleteImageCommandValidator
    : AbstractValidator<DeleteImageCommand>
{
    public DeleteImageCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Image ID must be greater than 0.");
    }
}