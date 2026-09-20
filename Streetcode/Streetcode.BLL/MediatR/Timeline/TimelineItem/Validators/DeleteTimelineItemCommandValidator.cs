using FluentValidation;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class DeleteTimelineItemCommandValidator : AbstractValidator<DeleteTimelineItemCommand>
{
    public DeleteTimelineItemCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Timeline item");
    }
}
