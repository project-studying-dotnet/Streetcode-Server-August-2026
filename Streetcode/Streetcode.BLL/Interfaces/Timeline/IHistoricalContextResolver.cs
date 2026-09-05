using FluentResults;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.DAL.Entities.Timeline;

namespace Streetcode.BLL.Interfaces.Timeline;

public interface IHistoricalContextResolver
{
    Task<Result<IReadOnlyCollection<HistoricalContextTimeline>>> ResolveAsync(
        IEnumerable<HistoricalContextDTO> requestedContexts);
}