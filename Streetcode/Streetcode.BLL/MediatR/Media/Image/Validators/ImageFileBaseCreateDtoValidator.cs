using System.Buffers.Text;
using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Media.Images;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class ImageFileBaseCreateDtoValidator
    : AbstractValidator<ImageFileBaseCreateDTO>
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>>
        AllowedFileTypes =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "jpg",
                    "jpeg",
                },
                ["image/png"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "png",
                },
                ["image/gif"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "gif",
                },
            };

    public ImageFileBaseCreateDtoValidator()
    {
        RuleFor(image => image.BaseFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .Must(base64 =>
                base64 is not null &&
                Base64.IsValid(base64.AsSpan()))
            .WithMessage(ErrorMessages.MustBeValidBase64);

        RuleFor(image => image.MimeType)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                ImageEntity.MimeTypeMaxLength,
                "Image MIME type");

        RuleFor(image => image.Extension)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required);

        RuleFor(image => image.Extension)
            .Must((image, _) => HaveSupportedFileType(image))
            .WithMessage(ErrorMessages.ImageMimeTypeAndExtensionNotSupported)
            .When(image =>
                !string.IsNullOrWhiteSpace(image.MimeType) &&
                !string.IsNullOrWhiteSpace(image.Extension));
    }

    private static bool HaveSupportedFileType(
        ImageFileBaseCreateDTO image)
    {
        string extension = image.Extension!
            .Trim()
            .TrimStart('.');

        return AllowedFileTypes.TryGetValue(
                   image.MimeType!.Trim(),
                   out HashSet<string>? extensions) &&
               extensions.Contains(extension);
    }
}
