using FluentValidation;
using Streetcode.BLL.MediatR.Media.Audio.GetById;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class GetAudioByIdQueryValidator
    : AbstractValidator<GetAudioByIdQuery>
{
    public GetAudioByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Audio ID must be greater than 0.");
    }
}