using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Create;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Validators;

public sealed class CreateRelatedFigureCommandValidator
    : AbstractValidator<CreateRelatedFigureCommand>
{
    public CreateRelatedFigureCommandValidator()
    {
        RuleFor(command => command.ObserverId)
            .GreaterThan(0)
            .WithMessage("Observer id must be greater than zero.");

        RuleFor(command => command.TargetId)
            .GreaterThan(0)
            .WithMessage("Target id must be greater than zero.");
    }
}