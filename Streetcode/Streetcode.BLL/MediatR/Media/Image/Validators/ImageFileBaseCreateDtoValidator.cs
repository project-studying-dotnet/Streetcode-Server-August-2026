using System.Buffers.Text;
using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.Media.Images;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class ImageFileBaseCreateDtoValidator
    : AbstractValidator<ImageFileBaseCreateDTO>
{
    public ImageFileBaseCreateDtoValidator()
    {
        RuleFor(image => image.BaseFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Image content is required.")
            .Must(base64 =>
                base64 is not null &&
                Base64.IsValid(base64.AsSpan()))
            .WithMessage("Image content must be valid Base64.");

        RuleFor(image => image.MimeType)
            .NotEmpty()
            .WithMessage("Image MIME type is required.")
            .MustNotExceedLength(
                ImageEntity.MimeTypeMaxLength,
                "Image MIME type");

        RuleFor(image => image.Extension)
            .NotEmpty()
            .WithMessage("Image extension is required.");
    }
}