using FluentValidation;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class AttachStreetcodeArtCommandValidator : AbstractValidator<AttachStreetcodeArtCommand>
{
    public AttachStreetcodeArtCommandValidator(IValidator<StreetcodeArtAttachDto> attachValidator)
    {
        RuleFor(command => command.Attach)
            .NotNull()
            .WithMessage("Attach data is required.")
            .SetValidator(attachValidator);
    }
}
