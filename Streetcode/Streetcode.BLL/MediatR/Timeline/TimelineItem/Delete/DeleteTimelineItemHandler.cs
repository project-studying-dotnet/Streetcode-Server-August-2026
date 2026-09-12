using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Delete;

public class DeleteTimelineItemHandler : IRequestHandler<DeleteTimelineItemCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public DeleteTimelineItemHandler(IRepositoryWrapper repositoryWrapper, ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        DeleteTimelineItemCommand request,
        CancellationToken cancellationToken)
    {
        var timelineItem = await _repositoryWrapper.TimelineRepository
            .GetFirstOrDefaultAsync(
                predicate: timelineItem => timelineItem.Id == request.Id);
        if (timelineItem is null)
        {
            string errorMsg = $"Cannot find a timeline item with corresponding id: {request.Id}";
            _logger.LogError(request, errorMsg);
            return Result.Fail<Unit>(errorMsg);
        }

        _repositoryWrapper.TimelineRepository.Delete(timelineItem);
        var result = await _repositoryWrapper.SaveChangesAsync();

        if (result <= 0)
        {
            string errorMsg = "Failed to delete timeline item.";
            _logger.LogError(request, errorMsg);
            return Result.Fail<Unit>(errorMsg);
        }

        return Result.Ok(Unit.Value);
    }
}
