using FluentValidation;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.MediatR.Media.Image.Create;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class CreateImageCommandValidator
    : AbstractValidator<CreateImageCommand>
{
    public CreateImageCommandValidator(
        IValidator<ImageFileBaseCreateDTO> imageValidator)
    {
        RuleFor(command => command.Image)
            .NotNull()
            .WithName("Image")
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(imageValidator);
    }
}