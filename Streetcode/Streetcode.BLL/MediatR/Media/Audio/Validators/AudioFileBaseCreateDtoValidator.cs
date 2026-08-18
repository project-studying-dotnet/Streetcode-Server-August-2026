using FluentValidation;
using Streetcode.BLL.DTO.Media.Audio;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class AudioFileBaseCreateDtoValidator
    : AbstractValidator<AudioFileBaseCreateDTO>
{
    public AudioFileBaseCreateDtoValidator()
    {
        RuleFor(audio => audio.Title)
            .MaximumLength(100)
            .WithMessage("Audio title must not exceed 100 characters.");

        RuleFor(audio => audio.BaseFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Audio content is required.")
            .Must(base64 =>
            {
                try
                {
                    Convert.FromBase64String(base64!);
                    return true;
                }
                catch (FormatException)
                {
                    return false;
                }
            })
            .WithMessage("Audio content must be valid Base64.");

        RuleFor(audio => audio.MimeType)
            .NotEmpty()
            .WithMessage("Audio MIME type is required.")
            .MaximumLength(10)
            .WithMessage("Audio MIME type must not exceed 10 characters.");

        RuleFor(audio => audio.Extension)
            .NotEmpty()
            .WithMessage("Audio extension is required.");
    }
}