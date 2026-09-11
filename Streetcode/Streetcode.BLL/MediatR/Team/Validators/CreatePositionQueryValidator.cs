using FluentValidation;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.Create;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class CreatePositionQueryValidator
    : AbstractValidator<CreatePositionQuery>
{
    public CreatePositionQueryValidator(
        IValidator<PositionDTO> positionValidator)
    {
        RuleFor(query => query.position)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(positionValidator);
    }
}