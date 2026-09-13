using FluentValidation;
using Streetcode.BLL.MediatR.Media.Art.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class DeleteArtCommandValidator : AbstractValidator<DeleteArtCommand>
{
    public DeleteArtCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Art");
    }
}
