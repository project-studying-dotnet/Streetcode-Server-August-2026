using System.Buffers.Text;
using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Media.Audio;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class AudioFileBaseCreateDtoValidator
    : AbstractValidator<AudioFileBaseCreateDTO>
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>>
        AllowedFileTypes =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["audio/mpeg"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "mp3",
                },
            };

    public AudioFileBaseCreateDtoValidator()
    {
        RuleFor(audio => audio.Title)
            .MustNotExceedLength(
                AudioEntity.TitleMaxLength,
                "Audio title");

        RuleFor(audio => audio.BaseFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Audio content is required.")
            .Must(base64 =>
                base64 is not null &&
                Base64.IsValid(base64.AsSpan()))
            .WithMessage("Audio content must be valid Base64.");

        RuleFor(audio => audio.MimeType)
            .NotEmpty()
            .WithMessage("Audio MIME type is required.")
            .MustNotExceedLength(
                AudioEntity.MimeTypeMaxLength,
                "Audio MIME type");

        RuleFor(audio => audio.Extension)
            .NotEmpty()
            .WithMessage("Audio extension is required.");

        RuleFor(audio => audio.Extension)
            .Must((audio, _) => HaveSupportedFileType(audio))
            .WithMessage(
                "Audio MIME type and extension combination is not supported.")
            .When(audio =>
                !string.IsNullOrWhiteSpace(audio.MimeType) &&
                !string.IsNullOrWhiteSpace(audio.Extension));
    }

    private static bool HaveSupportedFileType(
        AudioFileBaseCreateDTO audio)
    {
        string extension = audio.Extension!
            .Trim()
            .TrimStart('.');

        return AllowedFileTypes.TryGetValue(
                   audio.MimeType!.Trim(),
                   out HashSet<string>? extensions) &&
               extensions.Contains(extension);
    }
}
