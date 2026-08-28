using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;
using ImageDetailsEntity = Streetcode.DAL.Entities.Media.Images.ImageDetails;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Create;

public class CreateFactHandler : IRequestHandler<CreateFactCommand, Result<FactDto>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _loggerService;

    public CreateFactHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService loggerService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _loggerService = loggerService;
    }

    public async Task<Result<FactDto>> Handle(CreateFactCommand request, CancellationToken cancellationToken)
    {
        var streetcode = await _repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(
                predicate: s => s.Id == request.Fact.StreetcodeId);

        if (streetcode is null)
        {
            string errorMsg =
                $"Cannot find streetcode with id: {request.Fact.StreetcodeId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        var image = await _repositoryWrapper.ImageRepository.GetFirstOrDefaultAsync(
            predicate: i => i.Id == request.Fact.ImageId,
            include: query => query
                .Include(i => i.ImageDetails!));

        if (image is null)
        {
            var errorMsg =
                $"Cannot find image with id: {request.Fact.ImageId}";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        var existingFacts = await _repositoryWrapper.FactRepository.GetAllAsync(
            predicate: f => f.StreetcodeId == request.Fact.StreetcodeId);

        int nextDisplayOrder = existingFacts.Any()
            ? existingFacts.Max(f => f.DisplayOrder) + 1
            : 1;

        var fact = _mapper.Map<FactEntity>(request.Fact);
        fact.DisplayOrder = nextDisplayOrder;
        fact.Title = request.Fact.Title.Trim();
        fact.FactContent = request.Fact.FactContent.Trim();

        string? imageAlt =
            string.IsNullOrWhiteSpace(request.Fact.ImageAlt)
                ? null
                : request.Fact.ImageAlt.Trim();

        if (imageAlt is not null)
        {
            if (image.ImageDetails is null)
            {
                var imageDetailsEntity = new ImageDetailsEntity
                {
                    ImageId = image.Id,
                    Alt = imageAlt,
                };
                image.ImageDetails = imageDetailsEntity;
                await _repositoryWrapper.ImageDetailsRepository.CreateAsync(imageDetailsEntity);
            }
            else
            {
                image.ImageDetails.Alt = imageAlt;
                _repositoryWrapper.ImageDetailsRepository.Update(image.ImageDetails);
            }
        }

        await _repositoryWrapper.FactRepository.CreateAsync(fact);
        bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            var errorMsg = "Failed to create fact";
            _loggerService.LogError(request, errorMsg);
            return Result.Fail<FactDto>(new Error(errorMsg));
        }

        var createdFactDto = _mapper.Map<FactDto>(fact);
        createdFactDto.ImageAlt = image.ImageDetails?.Alt;

        return Result.Ok(createdFactDto);
    }
}
