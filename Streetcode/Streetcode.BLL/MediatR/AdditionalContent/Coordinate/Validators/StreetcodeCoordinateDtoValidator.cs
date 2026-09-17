using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class StreetcodeCoordinateDtoValidator
    : AbstractValidator<StreetcodeCoordinateDTO>
{
    public StreetcodeCoordinateDtoValidator()
    {
        RuleFor(coordinate => coordinate.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(coordinate => coordinate.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(coordinate => coordinate.Longtitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}
