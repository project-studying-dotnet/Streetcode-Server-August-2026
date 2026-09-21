using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.Resources;

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
            .WithName("Latitude")
            .WithMessage(ErrorMessages.LatitudeMustBeBetween);

        RuleFor(coordinate => coordinate.Longtitude)
            .InclusiveBetween(-180, 180)
            .WithName("Longitude")
            .WithMessage(ErrorMessages.LongitudeMustBeBetween);
    }
}
