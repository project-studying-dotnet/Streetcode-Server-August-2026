using FluentValidation;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.Create;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class CreatePositionQueryValidator
    : AbstractValidator<CreatePositionQuery>
{
    public CreatePositionQueryValidator(
        IValidator<PositionDTO> positionValidator)
    {
        RuleFor(query => query.position)
            .NotNull()
            .WithMessage("Position cannot be null.")
            .SetValidator(positionValidator);
    }
}