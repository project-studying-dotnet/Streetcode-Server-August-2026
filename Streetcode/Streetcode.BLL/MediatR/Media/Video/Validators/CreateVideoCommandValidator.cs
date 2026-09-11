using FluentValidation;
using Streetcode.BLL.MediatR.Media.Video.Create;

namespace Streetcode.BLL.MediatR.Media.Video.Validators;

public sealed class CreateVideoCommandValidator
    : AbstractValidator<CreateVideoCommand>
{
    public CreateVideoCommandValidator()
    {
        RuleFor(command => command.Video)
            .NotNull()
            .WithMessage("Video is required.");

        When(command => command.Video is not null, () =>
        {
            RuleFor(command => command.Video.Url)
                .NotEmpty()
                .WithMessage("Video URL is required.")
                .Must(YouTubeUrlHelper.IsValid)
                .WithMessage("Only YouTube URLs are allowed.");
        });
    }
}