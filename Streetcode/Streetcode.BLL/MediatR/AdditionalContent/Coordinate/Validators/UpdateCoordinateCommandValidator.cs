using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class UpdateCoordinateCommandValidator
    : AbstractValidator<UpdateCoordinateCommand>
{
    public UpdateCoordinateCommandValidator(
        IValidator<StreetcodeCoordinateDTO> coordinateValidator)
    {
        RuleFor(command => command.StreetcodeCoordinate)
            .NotNull()
            .WithMessage("Coordinate is required.")
            .SetValidator(coordinateValidator);

        RuleFor(command => command.StreetcodeCoordinate.Id)
            .MustBeValidId("Coordinate")
            .When(command => command.StreetcodeCoordinate is not null);
    }
}
