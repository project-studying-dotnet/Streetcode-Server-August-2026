using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Media.Art.Delete;

public class DeleteArtHandler : IRequestHandler<DeleteArtCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _loggerService;

    public DeleteArtHandler(IRepositoryWrapper repositoryWrapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _loggerService = loggerService;
    }

    public async Task<Result<Unit>> Handle(DeleteArtCommand request, CancellationToken cancellationToken)
    {
        var art = await _repositoryWrapper.ArtRepository
            .GetFirstOrDefaultAsync(
                predicate: a => a.Id == request.Id,
                include: query => query
                    .Include(a => a.StreetcodeArts));

        if (art is null)
        {
            var errorMsg = $"Cannot find art with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        if (art.StreetcodeArts.Count > 0)
        {
            var errorMsg = $"Cannot delete art with id: {request.Id}, because it is already published for a streetcode";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        _repositoryWrapper.ArtRepository.Delete(art);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg = $"Failed to delete art with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        return Result.Ok(Unit.Value);
    }
}
