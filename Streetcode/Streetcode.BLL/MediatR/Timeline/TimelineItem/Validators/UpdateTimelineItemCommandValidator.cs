using FluentValidation;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class UpdateTimelineItemCommandValidator
    : AbstractValidator<UpdateTimelineItemCommand>
{
    public UpdateTimelineItemCommandValidator(
        IValidator<TimelineItemCreateUpdateDto> timelineItemValidator)
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Timeline item");

        RuleFor(command => command.TimelineItem)
            .NotNull()
            .WithMessage("Timeline item is required.")
            .SetValidator(timelineItemValidator);
    }
}
