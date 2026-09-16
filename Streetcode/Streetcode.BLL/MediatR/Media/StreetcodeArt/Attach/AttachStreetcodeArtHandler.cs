using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

using Entity = Streetcode.DAL.Entities.Streetcode.StreetcodeArt;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;

public class AttachStreetcodeArtHandler : IRequestHandler<AttachStreetcodeArtCommand, Result<StreetcodeArtDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public AttachStreetcodeArtHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<StreetcodeArtDTO>> Handle(AttachStreetcodeArtCommand request, CancellationToken cancellationToken)
    {
        int streetcodeId = request.Attach.StreetcodeId;
        int artId = request.Attach.ArtId;

        var streetcode = await _repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(predicate: s => s.Id == streetcodeId);

        if (streetcode is null)
        {
            var errorMsg = $"Cannot find streetcode with id: {streetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<StreetcodeArtDTO>(new Error(errorMsg));
        }

        var art = await _repositoryWrapper.ArtRepository
            .GetFirstOrDefaultAsync(
                predicate: a => a.Id == artId,
                include: query => query
                    .Include(a => a.Image!));

        if (art is null)
        {
            var errorMsg = $"Cannot find art with id: {artId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<StreetcodeArtDTO>(new Error(errorMsg));
        }

        var existingLink = await _repositoryWrapper.StreetcodeArtRepository
            .GetFirstOrDefaultAsync(
                predicate: sa => sa.StreetcodeId == streetcodeId && sa.ArtId == artId);

        if (existingLink is not null)
        {
            var errorMsg =
                $"Art with id: {artId} is already attached to streetcode with id: {streetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<StreetcodeArtDTO>(new Error(errorMsg));
        }

        var existingLinks = (await _repositoryWrapper.StreetcodeArtRepository
            .GetAllAsync(predicate: sa => sa.StreetcodeId == streetcodeId))
            .ToList();

        int nextIndex = existingLinks.Count > 0
            ? existingLinks.Max(sa => sa.Index) + 1
            : 1;

        var link = new Entity
        {
            StreetcodeId = streetcodeId,
            ArtId = artId,
            Index = nextIndex,
            Art = art,
        };

        await _repositoryWrapper.StreetcodeArtRepository.CreateAsync(link);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to attach art with id: {artId} to streetcode with id: {streetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<StreetcodeArtDTO>(new Error(errorMsg));
        }

        return Result.Ok(_mapper.Map<StreetcodeArtDTO>(link));
    }
}
