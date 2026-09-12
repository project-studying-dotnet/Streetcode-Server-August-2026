using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Audio.GetById;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class GetAudioByIdQueryValidator
    : AbstractValidator<GetAudioByIdQuery>
{
    public GetAudioByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Audio");
    }
}