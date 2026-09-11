using FluentValidation;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.Create;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class CreateTeamLinkQueryValidator
    : AbstractValidator<CreateTeamLinkQuery>
{
    public CreateTeamLinkQueryValidator(
        IValidator<TeamMemberLinkDTO> linkValidator)
    {
        RuleFor(query => query.teamMember)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(linkValidator);
    }
}