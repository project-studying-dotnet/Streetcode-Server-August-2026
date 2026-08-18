using FluentValidation;
using Streetcode.BLL.MediatR.Media.Audio.Delete;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class DeleteAudioCommandValidator
    : AbstractValidator<DeleteAudioCommand>
{
    public DeleteAudioCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Audio ID must be greater than 0.");
    }
}