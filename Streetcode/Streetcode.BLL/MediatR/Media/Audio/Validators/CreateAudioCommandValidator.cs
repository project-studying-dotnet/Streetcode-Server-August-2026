using FluentValidation;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.MediatR.Media.Audio.Create;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class CreateAudioCommandValidator
    : AbstractValidator<CreateAudioCommand>
{
    public CreateAudioCommandValidator(
        IValidator<AudioFileBaseCreateDTO> audioValidator)
    {
        RuleFor(command => command.Audio)
            .NotNull()
            .WithMessage("Audio cannot be null.")
            .SetValidator(audioValidator);
    }
}