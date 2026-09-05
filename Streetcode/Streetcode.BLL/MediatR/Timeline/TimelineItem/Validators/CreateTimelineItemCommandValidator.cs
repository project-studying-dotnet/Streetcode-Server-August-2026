using FluentValidation;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class CreateTimelineItemCommandValidator : AbstractValidator<CreateTimelineItemCommand>
{
    public CreateTimelineItemCommandValidator(IValidator<TimelineItemCreateUpdateDto> timelineItemValidator)
    {
        RuleFor(command => command.TimelineItem)
            .NotNull()
            .WithMessage("Timeline item is required.")
            .SetValidator(timelineItemValidator);
    }
}
