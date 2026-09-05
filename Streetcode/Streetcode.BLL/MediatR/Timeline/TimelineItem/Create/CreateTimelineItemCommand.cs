using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Timeline;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;

public record CreateTimelineItemCommand(TimelineItemCreateUpdateDto TimelineItem)
    : IRequest<Result<TimelineItemDTO>>;