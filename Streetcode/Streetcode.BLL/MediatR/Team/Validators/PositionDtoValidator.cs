using FluentValidation;
using Streetcode.BLL.DTO.Team;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class PositionDtoValidator
    : AbstractValidator<PositionDTO>
{
    public PositionDtoValidator()
    {
        RuleFor(position => position.Position)
            .NotEmpty()
            .WithMessage("Position name is required.")
            .MaximumLength(50)
            .WithMessage("Position name must not exceed 50 characters.");
    }
}