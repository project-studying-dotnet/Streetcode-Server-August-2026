using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using HistoricalContextEntity =
    Streetcode.DAL.Entities.Timeline.HistoricalContext;
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

    public UpdateTimelineItemHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
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

        var requestedContexts = request.TimelineItem.HistoricalContexts.ToList();
        var existingContextIds = requestedContexts
            .Where(context => context.Id > 0)
            .Select(context => context.Id)
            .Distinct()
            .ToList();
        var newContextTitles = requestedContexts
            .Where(context => context.Id == 0)
            .Select(context => context.Title.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingContexts = (await _repositoryWrapper
            .HistoricalContextRepository
            .GetAllAsync(
                predicate: context =>
                    existingContextIds.Contains(context.Id)))
            .ToList();

        var foundContextIds = existingContexts
            .Select(context => context.Id)
            .ToHashSet();

        var missingContextIds = existingContextIds
            .Where(contextId => !foundContextIds.Contains(contextId))
            .ToList();

        if (missingContextIds.Count > 0)
        {
            string errorMessage =
                $"Cannot find historical contexts with IDs: " +
                $"{string.Join(", ", missingContextIds)}.";

            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        var conflictingContexts = (await _repositoryWrapper
            .HistoricalContextRepository
            .GetAllAsync(
                predicate: context =>
                    newContextTitles.Contains(context.Title)))
            .ToList();

        var conflictingContextTitles = conflictingContexts
            .Select(context => context.Title)
            .ToList();

        if (conflictingContextTitles.Count > 0)
        {
            string errorMessage =
                $"Historical contexts with titles already exist: " +
                $"{string.Join(", ", conflictingContextTitles)}.";

            _logger.LogError(request, errorMessage);
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        var newContexts = newContextTitles
            .Select(title => new HistoricalContextEntity
            {
                Title = title
            })
            .ToList();

        _mapper.Map<TimelineItemCreateUpdateDTO, TimelineItemEntity>(
            request.TimelineItem,
            timelineItem);

        timelineItem.Title = timelineItem.Title.Trim();
        timelineItem.Description = timelineItem.Description.Trim();

        var requestedContextIdSet = existingContextIds.ToHashSet();
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

        foreach (int contextId in existingContextIds)
        {
            if (currentContextIds.Contains(contextId))
            {
                continue;
            }

            timelineItem.HistoricalContextTimelines.Add(
                new HistoricalContextTimelineEntity
                {
                    TimelineId = timelineItem.Id,
                    HistoricalContextId = contextId
                });
        }

        foreach (HistoricalContextEntity newContext in newContexts)
        {
            timelineItem.HistoricalContextTimelines.Add(
                new HistoricalContextTimelineEntity
                {
                    TimelineId = timelineItem.Id,
                    HistoricalContext = newContext
                });
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
