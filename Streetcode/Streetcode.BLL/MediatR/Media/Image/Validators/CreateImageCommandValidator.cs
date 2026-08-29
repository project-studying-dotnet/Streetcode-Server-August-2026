using FluentValidation;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.MediatR.Media.Image.Create;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class CreateImageCommandValidator
    : AbstractValidator<CreateImageCommand>
{
    public CreateImageCommandValidator(
        IValidator<ImageFileBaseCreateDTO> imageValidator)
    {
        RuleFor(command => command.Image)
            .NotNull()
            .WithMessage("Image is required.")
            .SetValidator(imageValidator);
    }
}