using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Update;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class UpdateCoordinateCommandValidator
    : AbstractValidator<UpdateCoordinateCommand>
{
    public UpdateCoordinateCommandValidator(
        IValidator<StreetcodeCoordinateDTO> coordinateValidator)
    {
        RuleFor(command => command.StreetcodeCoordinate)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(coordinateValidator);

        RuleFor(command => command.StreetcodeCoordinate.Id)
            .MustBeValidId("Coordinate")
            .When(command => command.StreetcodeCoordinate is not null);
    }
}
