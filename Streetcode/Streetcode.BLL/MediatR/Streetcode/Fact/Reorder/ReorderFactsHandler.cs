using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;

public class ReorderFactsHandler : IRequestHandler<ReorderFactsCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _loggerService;

    public ReorderFactsHandler(IRepositoryWrapper repositoryWrapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _loggerService = loggerService;
    }

    public async Task<Result<Unit>> Handle(ReorderFactsCommand request, CancellationToken cancellationToken)
    {
        var orderedFactIds = request.Reorder.OrderedFactIds;

        if (orderedFactIds.Count != orderedFactIds.Distinct().Count())
        {
            const string errorMsg = "Fact order contains duplicate ids";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        var facts = (await _repositoryWrapper.FactRepository.GetAllAsync(
            predicate: fact =>
                fact.StreetcodeId == request.Reorder.StreetcodeId))
            .ToList();

        var storedFactIds = facts
            .Select(fact => fact.Id)
            .ToHashSet();

        if (!storedFactIds.SetEquals(orderedFactIds))
        {
            var errorMsg =
                $"Provided fact ids do not match facts of streetcode with id: {request.Reorder.StreetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        var factsById = facts.ToDictionary(fact => fact.Id);

        for (int index = 0; index < orderedFactIds.Count; index++)
        {
            int factId = orderedFactIds[index];
            factsById[factId].DisplayOrder = index + 1;
        }

        _repositoryWrapper.FactRepository.UpdateRange(facts);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to reorder facts for streetcode with id: {request.Reorder.StreetcodeId}";

            _loggerService.LogError(request, errorMsg);

            return Result.Fail<Unit>(new Error(errorMsg));
        }

        return Result.Ok(Unit.Value);
    }
}
