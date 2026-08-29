using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Create;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class CreateCoordinateCommandValidator
    : AbstractValidator<CreateCoordinateCommand>
{
    public CreateCoordinateCommandValidator(
        IValidator<StreetcodeCoordinateDTO> coordinateValidator)
    {
        RuleFor(command => command.StreetcodeCoordinate)
            .NotNull()
            .WithMessage("Coordinate is required.")
            .SetValidator(coordinateValidator);
    }
}
