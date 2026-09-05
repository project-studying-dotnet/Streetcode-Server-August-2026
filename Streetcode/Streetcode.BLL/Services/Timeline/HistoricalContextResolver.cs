using FluentResults;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.Interfaces.Timeline;
using Streetcode.DAL.Entities.Timeline;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.Services.Timeline;

public sealed class HistoricalContextResolver : IHistoricalContextResolver
{
    private readonly IRepositoryWrapper _repositoryWrapper;

    public HistoricalContextResolver(IRepositoryWrapper repositoryWrapper)
    {
        _repositoryWrapper = repositoryWrapper;
    }

    public async Task<Result<IReadOnlyCollection<HistoricalContextTimeline>>> ResolveAsync(
        IEnumerable<HistoricalContextDTO> requestedContexts)
    {
        var contexts = requestedContexts.ToList();
        var existingContextIds = contexts
            .Where(context => context.Id > 0)
            .Select(context => context.Id)
            .Distinct()
            .ToList();
        var newContextTitles = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var context in contexts.Where(context => context.Id == 0))
        {
            if (string.IsNullOrWhiteSpace(context.Title))
            {
                return Result.Fail<IReadOnlyCollection<HistoricalContextTimeline>>(
                    "Historical context title is required.");
            }

            newContextTitles.Add(context.Title.Trim());
        }

        var existingContexts = (await _repositoryWrapper.HistoricalContextRepository
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

            return Result.Fail<IReadOnlyCollection<HistoricalContextTimeline>>(errorMessage);
        }

        var conflictingContexts = (await _repositoryWrapper.HistoricalContextRepository
            .GetAllAsync(
                predicate: context =>
                    newContextTitles.Contains(context.Title)))
            .ToList();

        var conflictingContextTitles = conflictingContexts
            .Select(context => context.Title)
            .ToList();

        if (conflictingContextTitles.Count > 0)
        {
            string errorMessage = $"Historical contexts with titles already exist: {string.Join(", ", conflictingContextTitles)}.";

            return Result.Fail<IReadOnlyCollection<HistoricalContextTimeline>>(errorMessage);
        }

        var contextRelations = new List<HistoricalContextTimeline>();

        foreach (int contextId in existingContextIds)
        {
            contextRelations.Add(
                new HistoricalContextTimeline
                {
                    HistoricalContextId = contextId,
                });
        }

        foreach (string contextTitle in newContextTitles)
        {
            contextRelations.Add(
                new HistoricalContextTimeline
                {
                    HistoricalContext = new HistoricalContext
                    {
                        Title = contextTitle,
                    },
                });
        }

        return Result.Ok<IReadOnlyCollection<HistoricalContextTimeline>>(
            contextRelations);
    }
}
