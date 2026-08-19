using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Audio.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.Audio.Validators;

public sealed class GetAudioByStreetcodeIdQueryValidator
    : AbstractValidator<GetAudioByStreetcodeIdQuery>
{
    public GetAudioByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}