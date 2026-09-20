using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;

public class DetachStreetcodeArtHandler : IRequestHandler<DetachStreetcodeArtCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _loggerService;

    public DetachStreetcodeArtHandler(IRepositoryWrapper repositoryWrapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _loggerService = loggerService;
    }

    public async Task<Result<Unit>> Handle(DetachStreetcodeArtCommand request, CancellationToken cancellationToken)
    {
        var link = await _repositoryWrapper.StreetcodeArtRepository
            .GetFirstOrDefaultAsync(
                predicate: sa => sa.StreetcodeId == request.StreetcodeId && sa.ArtId == request.ArtId);

        if (link is null)
        {
            var errorMsg =
                $"Cannot find art with id: {request.ArtId} attached to streetcode with id: {request.StreetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        var followingLinks = (await _repositoryWrapper.StreetcodeArtRepository
            .GetAllAsync(
                predicate: sa =>
                    sa.StreetcodeId == request.StreetcodeId &&
                    sa.Index > link.Index))
            .ToList();

        foreach (var followingLink in followingLinks)
        {
            followingLink.Index -= 1;
        }

        _repositoryWrapper.StreetcodeArtRepository.UpdateRange(followingLinks);
        _repositoryWrapper.StreetcodeArtRepository.Delete(link);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to detach art with id: {request.ArtId} from streetcode with id: {request.StreetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        return Result.Ok(Unit.Value);
    }
}
