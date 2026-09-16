using FluentValidation;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class MoveStreetcodeArtCommandValidator : AbstractValidator<MoveStreetcodeArtCommand>
{
    public MoveStreetcodeArtCommandValidator()
    {
        RuleFor(command => command.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(command => command.ArtId)
            .MustBeValidId("Art");

        RuleFor(command => command.Direction)
            .NotNull()
            .WithMessage("Direction is required.")
            .IsInEnum()
            .WithMessage("Direction must be a valid move direction.");
    }
}
