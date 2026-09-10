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

                _mapper.Map(dto, streetcode);

                var tagIds = dto.Tags?.Select(t => t.Id).ToList() ?? new List<int>();
                var existingTags = await _repositoryWrapper.TagRepository.GetAllAsync(t => tagIds.Contains(t.Id));

                var missingTagIds = tagIds.Except(existingTags.Select(t => t.Id)).ToList();
                if (missingTagIds.Any())
                {
                    var errorMsg = $"Tag(s) not found: {string.Join(", ", missingTagIds)}";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(errorMsg);
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
                        _repositoryWrapper.StreetcodeTagIndexRepository.Create(new StreetcodeTagIndex
                        {
                            StreetcodeId = streetcode.Id,
                            TagId = tagDto.Id,
                            IsVisible = tagDto.IsVisible,
                            Index = tagDto.Index,
                        });
                    }
                }

                _repositoryWrapper.StreetcodeRepository.Update(streetcode);

                var animationImage = dto.AnimationImageId.HasValue
                    ? await _repositoryWrapper.ImageRepository.GetFirstOrDefaultAsync(i => i.Id == dto.AnimationImageId.Value)
                    : null;

                if (dto.AnimationImageId.HasValue && animationImage is null)
                {
                    const string errorMsg = "Animation image not found.";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(errorMsg);
                }

                if (animationImage is not null && animationImage.MimeType != "image/gif")
                {
                    const string gifErrorMsg = "Animation image must be a GIF file.";
                    _logger.LogError(request, gifErrorMsg);
                    return Result.Fail(gifErrorMsg);
                }

                if (animationImage is not null)
                {
                    _repositoryWrapper.ImageRepository.Attach(animationImage);
                }

                var blackAndWhiteImage = dto.BlackAndWhiteImageId.HasValue
                    ? await _repositoryWrapper.ImageRepository.GetFirstOrDefaultAsync(i => i.Id == dto.BlackAndWhiteImageId.Value)
                    : null;

                if (dto.BlackAndWhiteImageId.HasValue && blackAndWhiteImage is null)
                {
                    const string errorMsg = "Black and white image not found.";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(errorMsg);
                }

                if (blackAndWhiteImage is not null)
                {
                    _repositoryWrapper.ImageRepository.Attach(blackAndWhiteImage);
                }

                var relatedImage = dto.RelatedFigureImageId.HasValue
                    ? await _repositoryWrapper.ImageRepository.GetFirstOrDefaultAsync(i => i.Id == dto.RelatedFigureImageId.Value)
                    : null;

                if (dto.RelatedFigureImageId.HasValue && relatedImage is null)
                {
                    const string errorMsg = "Related figure image not found.";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(errorMsg);
                }

                if (relatedImage is not null)
                {
                    _repositoryWrapper.ImageRepository.Attach(relatedImage);
                }

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

                if (dto.AudioId.HasValue)
                {
                    var audio = await _repositoryWrapper.AudioRepository.GetFirstOrDefaultAsync(a => a.Id == dto.AudioId.Value);

                    if (audio is null)
                    {
                        const string errorMsg = "Audio not found.";
                        _logger.LogError(request, errorMsg);
                        return Result.Fail(errorMsg);
                    }

                    if (audio.MimeType != "audio/mpeg")
                    {
                        const string errorMsg = "Audio must be an MP3 file.";
                        _logger.LogError(request, errorMsg);
                        return Result.Fail(errorMsg);
                    }
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
