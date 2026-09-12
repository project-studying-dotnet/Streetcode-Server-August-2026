using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.Interfaces.Timeline;
using Streetcode.DAL.Repositories.Interfaces.Base;
using HistoricalContextTimelineEntity =
    Streetcode.DAL.Entities.Timeline.HistoricalContextTimeline;
using TimelineItemEntity =
    Streetcode.DAL.Entities.Timeline.TimelineItem;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Update;

public class UpdateTimelineItemHandler : IRequestHandler<UpdateTimelineItemCommand, Result<TimelineItemDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;
    private readonly IHistoricalContextResolver _historicalContextResolver;

    public UpdateTimelineItemHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger,
        IHistoricalContextResolver historicalContextResolver)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
        _historicalContextResolver = historicalContextResolver;
    }

    public async Task<Result<TimelineItemDTO>> Handle(
        UpdateTimelineItemCommand request,
        CancellationToken cancellationToken)
    {
        var timelineItem = await _repositoryWrapper.TimelineRepository
            .GetFirstOrDefaultAsync(
                predicate: item => item.Id == request.Id,
                include: query => query
                    .Include(item => item.HistoricalContextTimelines)
                    .ThenInclude(relation => relation.HistoricalContext!));

        if (timelineItem is null)
        {
            string errorMsg = $"Cannot find a timeline item with corresponding id: {request.Id}";
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        if (timelineItem.StreetcodeId != request.TimelineItem.StreetcodeId)
        {
            string errorMsg = $"Cannot move timeline item with id {request.Id} to another streetcode";
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        int streetcodeId = request.TimelineItem.StreetcodeId;

        var streetcode = await _repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(
                predicate: streetcode => streetcode.Id == streetcodeId);

        if (streetcode is null)
        {
            string errorMsg = $"Cannot find a streetcode with corresponding id: {streetcodeId}";
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        var contextResolution = await _historicalContextResolver
            .ResolveAsync(request.TimelineItem.HistoricalContexts);

        if (contextResolution.IsFailed)
        {
            string errorMessage = contextResolution.Errors[0].Message;
            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        _mapper.Map<TimelineItemCreateUpdateDto, TimelineItemEntity>(
            request.TimelineItem,
            timelineItem);

        timelineItem.Title = timelineItem.Title.Trim();
        timelineItem.Description = request.TimelineItem.Description.Trim();

        var requestedContextIdSet = contextResolution.Value
            .Where(relation => relation.HistoricalContextId > 0)
            .Select(relation => relation.HistoricalContextId)
            .ToHashSet();
        var relationsToRemove = timelineItem.HistoricalContextTimelines
            .Where(relation =>
                !requestedContextIdSet.Contains(relation.HistoricalContextId))
            .ToList();

        foreach (HistoricalContextTimelineEntity relation in relationsToRemove)
        {
            _repositoryWrapper.HistoricalContextTimelineRepository
                .Delete(relation);
            timelineItem.HistoricalContextTimelines.Remove(relation);
        }

        var currentContextIds = timelineItem.HistoricalContextTimelines
            .Select(relation => relation.HistoricalContextId)
            .ToHashSet();

        foreach (HistoricalContextTimelineEntity contextRelation in contextResolution.Value)
        {
            if (contextRelation.HistoricalContextId > 0 &&
                currentContextIds.Contains(contextRelation.HistoricalContextId))
            {
                continue;
            }

            contextRelation.TimelineId = timelineItem.Id;
            timelineItem.HistoricalContextTimelines.Add(contextRelation);
        }

        _repositoryWrapper.TimelineRepository.Update(timelineItem);

        int changedRecords;

        try
        {
            changedRecords = await _repositoryWrapper.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            const string errorMessage =
                "A historical context with one of the requested titles already exists.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }
        catch (DbUpdateException exception)
        {
            const string errorMessage = "Failed to update timeline item.";
            _logger.LogError(request, exception.ToString());
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        if (changedRecords <= 0)
        {
            const string errorMessage = "Failed to update timeline item.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        var updatedTimelineItem = await _repositoryWrapper.TimelineRepository
            .GetFirstOrDefaultAsync(
                predicate: item => item.Id == timelineItem.Id,
                include: query => query
                    .Include(item => item.HistoricalContextTimelines)
                    .ThenInclude(relation => relation.HistoricalContext!));

        if (updatedTimelineItem is null)
        {
            const string errorMessage =
                "Updated timeline item could not be retrieved.";
            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        var timelineItemDto = _mapper.Map<TimelineItemDTO>(updatedTimelineItem);
        return Result.Ok(timelineItemDto);
    }
}
