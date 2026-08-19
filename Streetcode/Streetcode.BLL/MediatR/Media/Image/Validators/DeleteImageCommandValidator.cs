using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Image.Delete;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class DeleteImageCommandValidator
    : AbstractValidator<DeleteImageCommand>
{
    public DeleteImageCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Image");
    }
}