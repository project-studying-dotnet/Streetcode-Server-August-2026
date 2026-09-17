using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Update;

public class UpdateStreetcodeHandler : IRequestHandler<UpdateStreetcodeCommand, Result<StreetcodeDTO>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;
    private readonly ICacheService _cacheService;

    public UpdateStreetcodeHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger,
        ICacheService cacheService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<StreetcodeDTO>> Handle(UpdateStreetcodeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.updatedStreetcode;

        try
        {
            var streetcode = await _repositoryWrapper.StreetcodeRepository
                .GetFirstOrDefaultAsync(s => s.Id == request.Id);

            if (streetcode is null)
            {
                string errorMsg = $"Cannot find a streetcode with id: {request.Id}";
                _logger.LogError(request, errorMsg);
                return Result.Fail(errorMsg);
            }

            if ((streetcode is PersonStreetcode && dto.StreetcodeType != StreetcodeType.Person) ||
                (streetcode is EventStreetcode && dto.StreetcodeType != StreetcodeType.Event))
            {
                const string errorMsg = "Streetcode type cannot be changed after creation.";
                _logger.LogError(request, errorMsg);
                return Result.Fail(errorMsg);
            }

            var uniquenessResult = await StreetcodeCreateUpdateChecks.EnsureIndexAndUrlAreUniqueAsync(
                _repositoryWrapper, dto.Index, dto.TransliterationUrl, streetcode.Id);

            if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(uniquenessResult, request, _logger) is { } uniquenessFailure)
            {
                return uniquenessFailure;
            }

            var oldIndex = streetcode.Index;
            var oldNormalizedUrl = streetcode.TransliterationUrl?.ToLowerInvariant() ?? string.Empty;

            _mapper.Map(dto, streetcode);

            var tagIds = dto.Tags?.Select(t => t.Id).ToList() ?? new List<int>();

            var tagsExistResult = await StreetcodeCreateUpdateChecks.ValidateTagsExistAsync(
                _repositoryWrapper, tagIds);

            if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(tagsExistResult, request, _logger) is { } tagsFailure)
            {
                return tagsFailure;
            }

            var existingTagIndexes = (await _repositoryWrapper.StreetcodeTagIndexRepository
                .GetAllAsync(ti => ti.StreetcodeId == streetcode.Id)).ToList();

            var tagIndexesToRemove = existingTagIndexes
                .Where(ti => !tagIds.Contains(ti.TagId))
                .ToList();

            _repositoryWrapper.StreetcodeTagIndexRepository.DeleteRange(tagIndexesToRemove);

            foreach (var tagDto in dto.Tags ?? Enumerable.Empty<StreetcodeTagDTO>())
            {
                var existingTagIndex = existingTagIndexes
                    .FirstOrDefault(ti => ti.TagId == tagDto.Id);

                if (existingTagIndex is not null)
                {
                    existingTagIndex.IsVisible = tagDto.IsVisible;
                    existingTagIndex.Index = tagDto.Index;
                    _repositoryWrapper.StreetcodeTagIndexRepository.Update(existingTagIndex);
                }
                else
                {
                    await _repositoryWrapper.StreetcodeTagIndexRepository.CreateAsync(new StreetcodeTagIndex
                    {
                        StreetcodeId = streetcode.Id,
                        TagId = tagDto.Id,
                        IsVisible = tagDto.IsVisible,
                        Index = tagDto.Index,
                    });
                }
            }

            _repositoryWrapper.StreetcodeRepository.Update(streetcode);

            var assetsResult = await StreetcodeRoleAssetResolver.ResolveAllAsync(
                _repositoryWrapper, dto.AnimationImageId, dto.BlackAndWhiteImageId, dto.RelatedFigureImageId, dto.AudioId);

            if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(assetsResult, request, _logger) is { } assetsFailure)
            {
                return assetsFailure;
            }

            var animationImage = assetsResult.Value.AnimationImage;
            var blackAndWhiteImage = assetsResult.Value.BlackAndWhiteImage;
            var relatedImage = assetsResult.Value.RelatedImage;

            var existingRoleImages = (await _repositoryWrapper.StreetcodeImageRepository
                .GetAllAsync(si => si.StreetcodeId == streetcode.Id && si.ImageAssignment != null)).ToList();

            var toRemove = existingRoleImages.Where(si =>
                (si.ImageAssignment == ImageAssignment.Animation && si.ImageId != dto.AnimationImageId) ||
                (si.ImageAssignment == ImageAssignment.BlackAndWhite && si.ImageId != dto.BlackAndWhiteImageId) ||
                (si.ImageAssignment == ImageAssignment.RelatedFigure && si.ImageId != dto.RelatedFigureImageId));

            _repositoryWrapper.StreetcodeImageRepository.DeleteRange(toRemove);

            var imagesToAdd = new List<StreetcodeImage>();
            if (animationImage is not null && !existingRoleImages.Any(si => si.ImageAssignment == ImageAssignment.Animation && si.ImageId == animationImage.Id))
            {
                imagesToAdd.Add(new StreetcodeImage { Image = animationImage, Streetcode = streetcode, ImageAssignment = ImageAssignment.Animation });
            }

            if (blackAndWhiteImage is not null && !existingRoleImages.Any(si => si.ImageAssignment == ImageAssignment.BlackAndWhite && si.ImageId == blackAndWhiteImage.Id))
            {
                imagesToAdd.Add(new StreetcodeImage { Image = blackAndWhiteImage, Streetcode = streetcode, ImageAssignment = ImageAssignment.BlackAndWhite });
            }

            if (relatedImage is not null && !existingRoleImages.Any(si => si.ImageAssignment == ImageAssignment.RelatedFigure && si.ImageId == relatedImage.Id))
            {
                imagesToAdd.Add(new StreetcodeImage { Image = relatedImage, Streetcode = streetcode, ImageAssignment = ImageAssignment.RelatedFigure });
            }

            await _repositoryWrapper.StreetcodeImageRepository.CreateRangeAsync(imagesToAdd);

            var success = await _repositoryWrapper.SaveChangesAsync() > 0;
            if (!success)
            {
                const string errorMsg = "Failed to update streetcode";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            var newNormalizedUrl = streetcode.TransliterationUrl?.ToLowerInvariant() ?? string.Empty;

            var cacheKeys = new HashSet<string>
            {
                $"streetcode:id:{streetcode.Id}",
                $"streetcode:short:{streetcode.Id}",
                $"streetcode:index:{oldIndex}",
                $"streetcode:index:{streetcode.Index}",
                $"streetcode:url:{oldNormalizedUrl}",
                $"streetcode:url:{newNormalizedUrl}"
            };

            await _cacheService.RemoveAsync(cacheKeys, CancellationToken.None);

            var dbo = _mapper.Map<StreetcodeDTO>(streetcode);

            return Result.Ok(dbo);
        }
        catch (Exception ex)
        {
            var detailedMessage = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(request, detailedMessage);
            return Result.Fail("An error occurred while saving the streetcode during update.");
        }
    }
}