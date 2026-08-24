using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class DeleteCoordinateCommandValidator
    : AbstractValidator<DeleteCoordinateCommand>
{
    public DeleteCoordinateCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Coordinate");
    }
}
