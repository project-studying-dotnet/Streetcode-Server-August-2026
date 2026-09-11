using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Team;
using Streetcode.DAL.Entities.Team;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class PositionDtoValidator
    : AbstractValidator<PositionDTO>
{
    public PositionDtoValidator()
    {
        RuleFor(position => position.Position)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                Positions.PositionMaxLength,
                "Position name");
    }
}