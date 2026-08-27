using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Team;
using Streetcode.DAL.Entities.Team;

namespace Streetcode.BLL.MediatR.Team.Validators;

public sealed class TeamMemberLinkDtoValidator
    : AbstractValidator<TeamMemberLinkDTO>
{
    public TeamMemberLinkDtoValidator()
    {
        RuleFor(link => link.TargetUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Team member URL is required.")
            .MustNotExceedLength(
                TeamMemberLink.TargetUrlMaxLength,
                "Team member URL")
            .MustBeValidHttpUrl("Team member URL");

        RuleFor(link => link.TeamMemberId)
            .MustBeValidId("Team member");

        RuleFor(link => link.LogoType)
            .IsInEnum()
            .WithMessage("Team member logo type is invalid.");
    }
}