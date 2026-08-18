using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Delete;

public class DeleteFactHandler : IRequestHandler<DeleteFactCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _loggerService;

    public DeleteFactHandler(IRepositoryWrapper repositoryWrapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _loggerService = loggerService;
    }

    public async Task<Result<Unit>> Handle(DeleteFactCommand request, CancellationToken cancellationToken)
    {
        var fact = await _repositoryWrapper.FactRepository
            .GetFirstOrDefaultAsync(
                predicate: f => f.Id == request.Id);

        if (fact is null)
        {
            var errorMsg = $"Cannot find fact with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        var followingFacts = (await _repositoryWrapper.FactRepository
            .GetAllAsync(
                predicate: f =>
                    f.StreetcodeId == fact.StreetcodeId &&
                    f.Index > fact.Index))
            .ToList();

        foreach (var followingFact in followingFacts)
        {
            followingFact.Index -= 1;
        }

        _repositoryWrapper.FactRepository.UpdateRange(followingFacts);
        _repositoryWrapper.FactRepository.Delete(fact);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg = $"Failed to delete fact with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        return Result.Ok(Unit.Value);
    }
}
