using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Create;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Validators;

public sealed class CreateRelatedFigureCommandValidator
    : AbstractValidator<CreateRelatedFigureCommand>
{
    public CreateRelatedFigureCommandValidator()
    {
        RuleFor(command => command.ObserverId)
            .MustBeValidId("Observer");

        RuleFor(command => command.TargetId)
            .MustBeValidId("Target");
    }
}