using FluentValidation;
using Streetcode.BLL.MediatR.Team.GetById;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class GetByIdTeamQueryValidator
    : AbstractValidator<GetByIdTeamQuery>
{
    public GetByIdTeamQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Team member ID must be greater than 0.");
    }
}