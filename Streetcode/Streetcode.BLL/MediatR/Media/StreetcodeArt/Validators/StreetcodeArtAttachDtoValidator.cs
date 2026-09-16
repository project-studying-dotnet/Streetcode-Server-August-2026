using FluentValidation;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class StreetcodeArtAttachDtoValidator : AbstractValidator<StreetcodeArtAttachDto>
{
    public StreetcodeArtAttachDtoValidator()
    {
        RuleFor(attach => attach.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(attach => attach.ArtId)
            .MustBeValidId("Art");
    }
}
