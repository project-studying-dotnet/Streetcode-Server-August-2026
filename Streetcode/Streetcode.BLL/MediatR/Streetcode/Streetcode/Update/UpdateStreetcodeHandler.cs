using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Update
{
    public class UpdateStreetcodeHandler : IRequestHandler<UpdateStreetcodeCommand, Result<StreetcodeDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerService _logger;

        public UpdateStreetcodeHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _mapper = mapper;
            _logger = logger;
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

                var animationImageResult = await StreetcodeRoleAssetResolver.ResolveRoleImageAsync(
                    _repositoryWrapper, dto.AnimationImageId, "Animation", "image/gif", "GIF");

                var blackAndWhiteImageResult = await StreetcodeRoleAssetResolver.ResolveRoleImageAsync(
                    _repositoryWrapper, dto.BlackAndWhiteImageId, "Black and white");

                var relatedImageResult = await StreetcodeRoleAssetResolver.ResolveRoleImageAsync(
                    _repositoryWrapper, dto.RelatedFigureImageId, "Related figure");

                var audioResult = await StreetcodeRoleAssetResolver.ResolveAudioAsync(_repositoryWrapper, dto.AudioId);

                if (StreetcodeCreateUpdateChecks.ExtractCombinedFailure<StreetcodeDTO>(
                        new ResultBase[] { animationImageResult, blackAndWhiteImageResult, relatedImageResult, audioResult },
                        request,
                        _logger) is { } assetsFailure)
                {
                    return assetsFailure;
                }

                var animationImage = animationImageResult.Value;
                var blackAndWhiteImage = blackAndWhiteImageResult.Value;
                var relatedImage = relatedImageResult.Value;

                var existingRoleImages = (await _repositoryWrapper.StreetcodeImageRepository
                    .GetAllAsync(si => si.StreetcodeId == streetcode.Id && si.ImageAssigment != null)).ToList();

                var toRemove = existingRoleImages.Where(si =>
                    (si.ImageAssigment == ImageAssigment.Animation && si.ImageId != dto.AnimationImageId) ||
                    (si.ImageAssigment == ImageAssigment.Blackandwhite && si.ImageId != dto.BlackAndWhiteImageId) ||
                    (si.ImageAssigment == ImageAssigment.Relatedfigure && si.ImageId != dto.RelatedFigureImageId));

                _repositoryWrapper.StreetcodeImageRepository.DeleteRange(toRemove);

                var imagesToAdd = new List<StreetcodeImage>();
                if (animationImage is not null && !existingRoleImages.Any(si => si.ImageAssigment == ImageAssigment.Animation && si.ImageId == animationImage.Id))
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = animationImage, Streetcode = streetcode, ImageAssigment = ImageAssigment.Animation });
                }

                if (blackAndWhiteImage is not null && !existingRoleImages.Any(si => si.ImageAssigment == ImageAssigment.Blackandwhite && si.ImageId == blackAndWhiteImage.Id))
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = blackAndWhiteImage, Streetcode = streetcode, ImageAssigment = ImageAssigment.Blackandwhite });
                }

                if (relatedImage is not null && !existingRoleImages.Any(si => si.ImageAssigment == ImageAssigment.Relatedfigure && si.ImageId == relatedImage.Id))
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = relatedImage, Streetcode = streetcode, ImageAssigment = ImageAssigment.Relatedfigure });
                }

                await _repositoryWrapper.StreetcodeImageRepository.CreateRangeAsync(imagesToAdd);

                var success = await _repositoryWrapper.SaveChangesAsync() > 0;
                if (!success)
                {
                    const string errorMsg = "Failed to update streetcode";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(new Error(errorMsg));
                }

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
}
