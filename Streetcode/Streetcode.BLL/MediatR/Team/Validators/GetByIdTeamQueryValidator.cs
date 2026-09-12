using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Team.GetById;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class GetByIdTeamQueryValidator
    : AbstractValidator<GetByIdTeamQuery>
{
    public GetByIdTeamQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Team member");
    }
}