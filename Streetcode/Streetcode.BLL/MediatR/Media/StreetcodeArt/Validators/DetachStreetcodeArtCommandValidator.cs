using FluentValidation;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class DetachStreetcodeArtCommandValidator : AbstractValidator<DetachStreetcodeArtCommand>
{
    public DetachStreetcodeArtCommandValidator()
    {
        RuleFor(command => command.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(command => command.ArtId)
            .MustBeValidId("Art");
    }
}
