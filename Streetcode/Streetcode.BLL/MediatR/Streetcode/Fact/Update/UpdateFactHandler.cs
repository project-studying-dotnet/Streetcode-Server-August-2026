using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using ImageDetailsEntity = Streetcode.DAL.Entities.Media.Images.ImageDetails;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Update;

public class UpdateFactHandler : IRequestHandler<UpdateFactCommand, Result<FactDto>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public UpdateFactHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<FactDto>> Handle(UpdateFactCommand request, CancellationToken cancellationToken)
    {
        var fact = await _repositoryWrapper.FactRepository
            .GetFirstOrDefaultAsync(
                predicate: f => f.Id == request.Id);

        if (fact is null)
        {
            var errorMsg =
                $"Cannot find fact with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        if (fact.StreetcodeId != request.Fact.StreetcodeId)
        {
            var errorMsg =
                $"Cannot move fact with id {request.Id} to another streetcode";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        var image = await _repositoryWrapper.ImageRepository
            .GetFirstOrDefaultAsync(
                predicate: f => f.Id == request.Fact.ImageId,
                include: query => query
                    .Include(f => f.ImageDetails));

        if (image is null)
        {
            var errorMsg =
                $"Cannot find image with id: {request.Fact.ImageId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        _mapper.Map(request.Fact, fact);
        fact.Title = request.Fact.Title.Trim();
        fact.FactContent = request.Fact.FactContent.Trim();

        var imageDescription = request.Fact.ImageDescription;
        if (string.IsNullOrWhiteSpace(imageDescription))
        {
            imageDescription = null;
        }
        else
        {
            imageDescription = imageDescription.Trim();
        }

        if (image.ImageDetails is null)
        {
            if (imageDescription is not null)
            {
                var imageDetailsEntity = new ImageDetailsEntity
                {
                    ImageId = image.Id,
                    Alt = imageDescription,
                };
                image.ImageDetails = imageDetailsEntity;
                await _repositoryWrapper.ImageDetailsRepository.CreateAsync(imageDetailsEntity);
            }
        }
        else
        {
            image.ImageDetails.Alt = imageDescription;
            _repositoryWrapper.ImageDetailsRepository.Update(image.ImageDetails);
        }

        _repositoryWrapper.FactRepository.Update(fact);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg =
                $"Failed to update fact with id: {request.Id}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        var factDto = _mapper.Map<FactDto>(fact);
        factDto.ImageDescription = image.ImageDetails?.Alt;

        return Result.Ok(factDto);
    }
}
