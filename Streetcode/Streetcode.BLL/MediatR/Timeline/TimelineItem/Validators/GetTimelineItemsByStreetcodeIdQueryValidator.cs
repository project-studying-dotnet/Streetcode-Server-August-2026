using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class GetTimelineItemsByStreetcodeIdQueryValidator
    : AbstractValidator<GetTimelineItemsByStreetcodeIdQuery>
{
    public GetTimelineItemsByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}