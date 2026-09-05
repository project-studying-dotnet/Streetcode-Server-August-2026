using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.Interfaces.Timeline;
using Streetcode.DAL.Repositories.Interfaces.Base;
using TimelineItemEntity =
    Streetcode.DAL.Entities.Timeline.TimelineItem;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;

public class CreateTimelineItemHandler : IRequestHandler<CreateTimelineItemCommand, Result<TimelineItemDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;
    private readonly IHistoricalContextResolver _historicalContextResolver;

    public CreateTimelineItemHandler(
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
        CreateTimelineItemCommand request,
        CancellationToken cancellationToken)
    {
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

        var timelineItem = _mapper.Map<TimelineItemEntity>(request.TimelineItem);

        timelineItem.Title = timelineItem.Title.Trim();
        timelineItem.Description = request.TimelineItem.Description.Trim();

        var contextResolution = await _historicalContextResolver
            .ResolveAsync(request.TimelineItem.HistoricalContexts);

        if (contextResolution.IsFailed)
        {
            string errorMsg = contextResolution.Errors[0].Message;
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        timelineItem.HistoricalContextTimelines.AddRange(contextResolution.Value);

        var createdTimelineItem = await _repositoryWrapper.TimelineRepository.CreateAsync(timelineItem);

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
            const string errorMessage = "Failed to create timeline item.";
            _logger.LogError(request, exception.ToString());
            return Result.Fail<TimelineItemDTO>(errorMessage);
        }

        if (changedRecords <= 0)
        {
            string errorMsg = "Failed to create timeline item.";
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        var savedTimelineItem = await _repositoryWrapper.TimelineRepository
            .GetFirstOrDefaultAsync(
                predicate: item => item.Id == createdTimelineItem.Id,
                include: query => query
                    .Include(item => item.HistoricalContextTimelines)
                    .ThenInclude(relation => relation.HistoricalContext!));

        if (savedTimelineItem is null)
        {
            string errorMsg = "Created timeline item could not be retrieved.";
            _logger.LogError(request, errorMsg);
            return Result.Fail<TimelineItemDTO>(errorMsg);
        }

        var timelineItemDto = _mapper.Map<TimelineItemDTO>(savedTimelineItem);
        return Result.Ok(timelineItemDto);
    }
}
