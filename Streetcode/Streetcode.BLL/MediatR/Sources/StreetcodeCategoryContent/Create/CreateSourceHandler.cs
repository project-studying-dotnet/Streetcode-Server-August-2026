using System.Security.Cryptography;
using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.Interfaces.Sources;
using Streetcode.DAL.Repositories.Interfaces.Base;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;
using SourceLinkCategoryEntity =
    Streetcode.DAL.Entities.Sources.SourceLinkCategory;
using StreetcodeCategoryContentEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Create;

public sealed class CreateSourceHandler
    : IRequestHandler<CreateSourceCommand, Result<StreetcodeCategoryContentDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;
    private readonly ISourceCategoryImageProcessor _imageProcessor;
    private readonly IBlobService _blobService;

    public CreateSourceHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger,
        ISourceCategoryImageProcessor imageProcessor,
        IBlobService blobService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
        _imageProcessor = imageProcessor;
        _blobService = blobService;
    }

    public async Task<Result<StreetcodeCategoryContentDTO>> Handle(
        CreateSourceCommand request,
        CancellationToken cancellationToken)
    {
        SourceCreateDTO source = request.SourceCreateDto;

        var streetcode = await _repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(
                predicate: item => item.Id == source.StreetcodeId);

        if (streetcode is null)
        {
            string errorMessage =
                $"Cannot find streetcode with id: {source.StreetcodeId}";

            _logger.LogError(request, errorMessage);

            return Result.Fail<StreetcodeCategoryContentDTO>(
                new Error(errorMessage));
        }

        StreetcodeCategoryContentEntity sourceEntity;
        string? createdBlobName = null;

        if (!source.SourceLinkCategoryId.HasValue)
        {
            string newCategoryTitle = source.NewCategoryTitle!.Trim();

            var categoryWithSameTitle = await _repositoryWrapper
                .SourceCategoryRepository
                .GetFirstOrDefaultAsync(
                    predicate: category =>
                        category.Title == newCategoryTitle);

            if (categoryWithSameTitle is not null)
            {
                const string duplicateTitleErrorMessage =
                    "Source category with this title already exists.";

                _logger.LogError(request, duplicateTitleErrorMessage);

                return Result.Fail<StreetcodeCategoryContentDTO>(
                    new Error(duplicateTitleErrorMessage));
            }

            var grayscaleImage = _imageProcessor.ConvertToGrayscale(
                source.NewCategoryImage!);

            byte[] grayscaleImageBytes =
                Convert.FromBase64String(grayscaleImage.BaseFormat!);

            string imageHash =
                Convert.ToHexString(SHA256.HashData(grayscaleImageBytes));

            var categoryWithSameImage = await _repositoryWrapper
                .SourceCategoryRepository
                .GetFirstOrDefaultAsync(
                    predicate: category =>
                        category.ImageHash == imageHash);

            if (categoryWithSameImage is not null)
            {
                const string duplicateImageErrorMessage =
                    "Source category with this image already exists.";

                _logger.LogError(request, duplicateImageErrorMessage);

                return Result.Fail<StreetcodeCategoryContentDTO>(
                    new Error(duplicateImageErrorMessage));
            }

            string blobStorageName = _blobService.SaveFileInStorage(
                grayscaleImage.BaseFormat!,
                newCategoryTitle,
                grayscaleImage.Extension!);

            var imageEntity = _mapper.Map<ImageEntity>(grayscaleImage);

            imageEntity.BlobName =
                $"{blobStorageName}.{grayscaleImage.Extension}";

            createdBlobName = imageEntity.BlobName;

            var newCategory = new SourceLinkCategoryEntity
            {
                Title = newCategoryTitle,
                ImageHash = imageHash,
                Image = imageEntity,
            };

            sourceEntity = new StreetcodeCategoryContentEntity
            {
                Text = source.Text,
                StreetcodeId = source.StreetcodeId,
                SourceLinkCategory = newCategory,
            };
        }
        else
        {
            int categoryId = source.SourceLinkCategoryId.Value;

            var category = await _repositoryWrapper.SourceCategoryRepository
                .GetFirstOrDefaultAsync(
                    predicate: item => item.Id == categoryId);

            if (category is null)
            {
                string errorMessage =
                    $"Cannot find source category with id: {categoryId}";

                _logger.LogError(request, errorMessage);

                return Result.Fail<StreetcodeCategoryContentDTO>(
                    new Error(errorMessage));
            }

            var existingSource = await _repositoryWrapper
                .StreetcodeCategoryContentRepository
                .GetFirstOrDefaultAsync(
                    predicate: item =>
                        item.StreetcodeId == source.StreetcodeId &&
                        item.SourceLinkCategoryId == categoryId);

            if (existingSource is not null)
            {
                const string errorMessage =
                    "This source category is already added to the streetcode.";

                _logger.LogError(request, errorMessage);

                return Result.Fail<StreetcodeCategoryContentDTO>(
                    new Error(errorMessage));
            }

            sourceEntity = new StreetcodeCategoryContentEntity
            {
                Text = source.Text,
                StreetcodeId = source.StreetcodeId,
                SourceLinkCategoryId = categoryId,
            };
        }

        await _repositoryWrapper.StreetcodeCategoryContentRepository
            .CreateAsync(sourceEntity);

        bool isSaved =
            await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            const string errorMessage =
                "Failed to create source block.";

            if (createdBlobName is not null)
            {
                _blobService.DeleteFileInStorage(createdBlobName);
            }

            _logger.LogError(request, errorMessage);

            return Result.Fail<StreetcodeCategoryContentDTO>(
                new Error(errorMessage));
        }

        var createdSource =
            _mapper.Map<StreetcodeCategoryContentDTO>(sourceEntity);

        return Result.Ok(createdSource);
    }
}
