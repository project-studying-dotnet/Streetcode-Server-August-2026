using FluentValidation;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.Create;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class CreateTeamLinkQueryValidator
    : AbstractValidator<CreateTeamLinkQuery>
{
    public CreateTeamLinkQueryValidator(
        IValidator<TeamMemberLinkDTO> linkValidator)
    {
        RuleFor(query => query.teamMember)
            .NotNull()
            .WithMessage("Team member link is required.")
            .SetValidator(linkValidator);
    }
}