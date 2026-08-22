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
                .Must(BeYoutubeUrl)
                .WithMessage("Only YouTube URLs are allowed.");
        });
    }

    private static bool BeYoutubeUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && (uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("m.youtube.com", StringComparison.OrdinalIgnoreCase));
    }
}
