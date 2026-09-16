using FluentResults;
using MediatR;
using Streetcode.BLL.Enums;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;

public class MoveStreetcodeArtHandler : IRequestHandler<MoveStreetcodeArtCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _loggerService;

    public MoveStreetcodeArtHandler(IRepositoryWrapper repositoryWrapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _loggerService = loggerService;
    }

    public async Task<Result<Unit>> Handle(MoveStreetcodeArtCommand request, CancellationToken cancellationToken)
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

        int targetIndex = request.Direction == MoveDirection.Forward
            ? link.Index + 1
            : link.Index - 1;

        var neighbor = await _repositoryWrapper.StreetcodeArtRepository
            .GetFirstOrDefaultAsync(
                predicate: sa => sa.StreetcodeId == request.StreetcodeId && sa.Index == targetIndex);

        if (neighbor is null)
        {
            string boundary = request.Direction == MoveDirection.Forward ? "last" : "first";
            var errorMsg = $"Cannot move art with id: {request.ArtId}, because it is already at the {boundary} position";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        (link.Index, neighbor.Index) = (neighbor.Index, link.Index);

        _repositoryWrapper.StreetcodeArtRepository.UpdateRange(new[] { link, neighbor });
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to move art with id: {request.ArtId} for streetcode with id: {request.StreetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<Unit>(new Error(errorMsg));
        }

        return Result.Ok(Unit.Value);
    }
}
