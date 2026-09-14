using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Media.Art.Update;

public class UpdateArtHandler : IRequestHandler<UpdateArtCommand, Result<ArtDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public UpdateArtHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<ArtDTO>> Handle(UpdateArtCommand request, CancellationToken cancellationToken)
    {
        var art = await _repositoryWrapper.ArtRepository
            .GetFirstOrDefaultAsync(predicate: a => a.Id == request.Id);

        if (art is null)
        {
            var errorMsg = $"Cannot find art with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        var image = await _repositoryWrapper.ImageRepository
            .GetFirstOrDefaultAsync(predicate: i => i.Id == request.Art.ImageId);

        if (image is null)
        {
            var errorMsg = $"Cannot find image with id: {request.Art.ImageId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        var existingArt = await _repositoryWrapper.ArtRepository
            .GetFirstOrDefaultAsync(predicate: a => a.ImageId == request.Art.ImageId);

        if (existingArt is not null && existingArt.Id != art.Id)
        {
            var errorMsg = $"Image with id: {request.Art.ImageId} is already used by another art";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        art.ImageId = request.Art.ImageId;
        art.Title = request.Art.Title?.Trim();
        art.Description = request.Art.Description?.Trim();

        _repositoryWrapper.ArtRepository.Update(art);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg = $"Failed to update art with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        return Result.Ok(_mapper.Map<ArtDTO>(art));
    }
}
