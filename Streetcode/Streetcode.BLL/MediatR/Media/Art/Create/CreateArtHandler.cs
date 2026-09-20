using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

using Entity = Streetcode.DAL.Entities.Media.Images.Art;

namespace Streetcode.BLL.MediatR.Media.Art.Create;

public class CreateArtHandler : IRequestHandler<CreateArtCommand, Result<ArtDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public CreateArtHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<ArtDTO>> Handle(CreateArtCommand request, CancellationToken cancellationToken)
    {
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

        if (existingArt is not null)
        {
            var errorMsg = $"Image with id: {request.Art.ImageId} is already used by another art";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        var art = _mapper.Map<Entity>(request.Art);
        art.Title = request.Art.Title?.Trim();
        art.Description = request.Art.Description?.Trim();

        await _repositoryWrapper.ArtRepository.CreateAsync(art);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            const string errorMsg = "Failed to create art";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<ArtDTO>(new Error(errorMsg));
        }

        return Result.Ok(_mapper.Map<ArtDTO>(art));
    }
}
