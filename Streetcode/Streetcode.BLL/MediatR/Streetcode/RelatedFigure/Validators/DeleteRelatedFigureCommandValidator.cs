using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Delete;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Validators;

public sealed class DeleteRelatedFigureCommandValidator
    : AbstractValidator<DeleteRelatedFigureCommand>
{
    public DeleteRelatedFigureCommandValidator()
    {
        RuleFor(command => command.ObserverId)
            .MustBeValidId("Observer");

        RuleFor(command => command.TargetId)
            .MustBeValidId("Target");
    }
}