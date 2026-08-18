using FluentValidation;
using Streetcode.BLL.DTO.Media.Images;

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
            .WithMessage("Image content must be valid Base64.");

        RuleFor(image => image.MimeType)
            .NotEmpty()
            .WithMessage("Image MIME type is required.")
            .MaximumLength(10)
            .WithMessage("Image MIME type must not exceed 10 characters.");

        RuleFor(image => image.Extension)
            .NotEmpty()
            .WithMessage("Image extension is required.");
    }
}