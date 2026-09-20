using System.Buffers.Text;
using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.Resources;
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
            .WithName("Audio base format")
            .WithMessage(ErrorMessages.Field_Required)
            .Must(base64 =>
                base64 is not null &&
                Base64.IsValid(base64.AsSpan()))
            .WithMessage(ErrorMessages.MustBeValidBase64);

        RuleFor(audio => audio.MimeType)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .WithName("Audio MIME type")
            .MustNotExceedLength(
                AudioEntity.MimeTypeMaxLength,
                "Audio MIME type");

        RuleFor(audio => audio.Extension)
            .NotEmpty()
            .WithName("Audio file extension")
            .WithMessage(ErrorMessages.Field_Required);

        RuleFor(audio => audio.Extension)
            .Must((audio, _) => HaveSupportedFileType(audio))
            .WithMessage(
                ErrorMessages.AudioMimeTypeAndExtensionNotSupported)
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
