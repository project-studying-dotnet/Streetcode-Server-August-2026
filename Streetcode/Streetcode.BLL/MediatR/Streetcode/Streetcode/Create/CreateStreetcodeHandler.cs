using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Create
{
    public class CreateStreetcodeHandler : IRequestHandler<CreateStreetcodeCommand, Result<StreetcodeDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerService _logger;

        public CreateStreetcodeHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<StreetcodeDTO>> Handle(CreateStreetcodeCommand request, CancellationToken cancellationToken)
        {
            var dto = request.newStreetcode;
            StreetcodeContent entity = dto.StreetcodeType == StreetcodeType.Person
                ? new PersonStreetcode()
                : new EventStreetcode();

            try
            {
                _mapper.Map(dto, entity);

                var uniquenessResult = await StreetcodeCreateUpdateChecks.EnsureIndexAndUrlAreUniqueAsync(
                    _repositoryWrapper, dto.Index, dto.TransliterationUrl);

                if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(uniquenessResult, request, _logger) is { } uniquenessFailure)
                {
                    return uniquenessFailure;
                }

                var tagIds = dto.Tags?.Select(t => t.Id).ToList() ?? new List<int>();

                var tagsExistResult = await StreetcodeCreateUpdateChecks.ValidateTagsExistAsync(
                    _repositoryWrapper, tagIds);

                if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(tagsExistResult, request, _logger) is { } tagsFailure)
                {
                    return tagsFailure;
                }

                var tagIndexesToAdd = dto.Tags?
                    .Select(tagDto => new StreetcodeTagIndex
                    {
                        TagId = tagDto.Id,
                        Streetcode = entity,
                        IsVisible = tagDto.IsVisible,
                        Index = tagDto.Index,
                    })
                    .ToList() ?? new List<StreetcodeTagIndex>();

                var assetsResult = await StreetcodeRoleAssetResolver.ResolveAllAsync(
                    _repositoryWrapper, dto.AnimationImageId, dto.BlackAndWhiteImageId, dto.RelatedFigureImageId, dto.AudioId);

                if (StreetcodeCreateUpdateChecks.ExtractFailure<StreetcodeDTO>(assetsResult, request, _logger) is { } assetsFailure)
                {
                    return assetsFailure;
                }

                var animationImage = assetsResult.Value.AnimationImage;
                var blackAndWhiteImage = assetsResult.Value.BlackAndWhiteImage;
                var relatedImage = assetsResult.Value.RelatedImage;

                var imagesToAdd = new List<StreetcodeImage>();
                if (animationImage is not null)
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = animationImage, Streetcode = entity, ImageAssigment = ImageAssigment.Animation });
                }

                if (blackAndWhiteImage is not null)
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = blackAndWhiteImage, Streetcode = entity, ImageAssigment = ImageAssigment.Blackandwhite });
                }

                if (relatedImage is not null)
                {
                    imagesToAdd.Add(new StreetcodeImage { Image = relatedImage, Streetcode = entity, ImageAssigment = ImageAssigment.Relatedfigure });
                }

                await _repositoryWrapper.StreetcodeRepository.CreateAsync(entity);
                await _repositoryWrapper.StreetcodeImageRepository.CreateRangeAsync(imagesToAdd);
                await _repositoryWrapper.StreetcodeTagIndexRepository.CreateRangeAsync(tagIndexesToAdd);
                var success = await _repositoryWrapper.SaveChangesAsync() > 0;

                if (!success)
                {
                    const string errorMsg = "Failed to create streetcode";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(new Error(errorMsg));
                }

                return Result.Ok(_mapper.Map<StreetcodeDTO>(entity));
            }
            catch (Exception ex)
            {
                var detailedMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(request, detailedMessage);
                return Result.Fail("An error occurred while saving the streetcode during creation.");
            }
        }
    }
}