using FluentValidation;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.MediatR.Media.Audio.Create;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class CreateAudioCommandValidator
    : AbstractValidator<CreateAudioCommand>
{
    public CreateAudioCommandValidator(
        IValidator<AudioFileBaseCreateDTO> audioValidator)
    {
        RuleFor(command => command.Audio)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(audioValidator);
    }
}