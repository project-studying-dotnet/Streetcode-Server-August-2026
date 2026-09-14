using FluentValidation;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.MediatR.Media.Art.Create;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class CreateArtCommandValidator : AbstractValidator<CreateArtCommand>
{
    public CreateArtCommandValidator(IValidator<ArtUpdateCreateDto> artValidator)
    {
        RuleFor(command => command.Art)
            .NotNull()
            .WithMessage("Art is required.")
            .SetValidator(artValidator);
    }
}
