using FluentValidation;

namespace Streetcode.Identity.Application.Features.Authentication.Refresh;

public sealed class RefreshSessionCommandValidator
    : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("RefreshToken.Required")
            .MaximumLength(512)
            .WithErrorCode("RefreshToken.TooLong");
    }
}
