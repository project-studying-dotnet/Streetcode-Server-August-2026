using FluentValidation;
using Streetcode.BLL.DTO.Team;

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
            .MaximumLength(255)
            .WithMessage("Team member URL must not exceed 255 characters.")
            .Must(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                    || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Team member URL must be a valid HTTP or HTTPS URL.");

        RuleFor(link => link.TeamMemberId)
            .GreaterThan(0)
            .WithMessage("Team member ID must be greater than 0.");

        RuleFor(link => link.LogoType)
            .IsInEnum()
            .WithMessage("Team member logo type is invalid.");
    }
}