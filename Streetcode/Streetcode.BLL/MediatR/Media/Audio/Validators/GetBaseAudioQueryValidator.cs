using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Audio.GetBaseAudio;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class GetBaseAudioQueryValidator
    : AbstractValidator<GetBaseAudioQuery>
{
    public GetBaseAudioQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Audio");
    }
}