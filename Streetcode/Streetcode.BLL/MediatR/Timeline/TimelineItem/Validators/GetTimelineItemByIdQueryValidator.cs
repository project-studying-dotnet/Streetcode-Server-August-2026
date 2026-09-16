using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetById;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class GetTimelineItemByIdQueryValidator
    : AbstractValidator<GetTimelineItemByIdQuery>
{
    public GetTimelineItemByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Timeline item");
    }
}