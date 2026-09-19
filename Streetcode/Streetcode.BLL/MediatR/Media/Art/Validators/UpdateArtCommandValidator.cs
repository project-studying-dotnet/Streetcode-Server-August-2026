using FluentValidation;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.MediatR.Media.Art.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class UpdateArtCommandValidator : AbstractValidator<UpdateArtCommand>
{
    public UpdateArtCommandValidator(IValidator<ArtUpdateCreateDto> artValidator)
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Art");

        RuleFor(command => command.Art)
            .NotNull()
            .WithMessage("Art is required.")
            .SetValidator(artValidator);
    }
}
