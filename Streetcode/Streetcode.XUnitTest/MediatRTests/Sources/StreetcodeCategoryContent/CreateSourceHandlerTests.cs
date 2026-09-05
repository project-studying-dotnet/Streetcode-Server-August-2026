using System.Linq.Expressions;
using System.Security.Cryptography;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.Interfaces.Sources;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Source;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using SourceLinkCategoryEntity =
    Streetcode.DAL.Entities.Sources.SourceLinkCategory;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;
using StreetcodeCategoryContentEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;
using StreetcodeEntity =
    Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

namespace Streetcode.XUnitTest.MediatRTests.Sources.StreetcodeCategoryContent;

public class CreateSourceHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<ISourceCategoryRepository> _sourceCategoryRepositoryMock = new();
    private readonly Mock<IStreetcodeCategoryContentRepository>
        _streetcodeCategoryContentRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ISourceCategoryImageProcessor> _imageProcessorMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();

    public CreateSourceHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SourceCategoryRepository)
            .Returns(_sourceCategoryRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeCategoryContentRepository)
            .Returns(_streetcodeCategoryContentRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenExistingCategoryIsValid_ShouldCreateSource()
    {
        const int categoryId = 10;
        var sourceDto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: categoryId,
            NewCategoryTitle: null,
            NewCategoryImage: null);
        var command = new CreateSourceCommand(sourceDto);
        var streetcode = new StreetcodeEntity
        {
            Id = sourceDto.StreetcodeId,
        };
        var category = new SourceLinkCategoryEntity
        {
            Id = categoryId,
        };
        var expectedDto = new StreetcodeCategoryContentDTO
        {
            Text = sourceDto.Text,
            StreetcodeId = sourceDto.StreetcodeId,
            SourceLinkCategoryId = category.Id,
        };
        StreetcodeCategoryContentEntity? createdSource = null;

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        _sourceCategoryRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceLinkCategoryEntity, bool>>>(),
                null))
            .ReturnsAsync(category);

        _streetcodeCategoryContentRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<
                    Func<StreetcodeCategoryContentEntity, bool>>>(),
                null))
            .ReturnsAsync((StreetcodeCategoryContentEntity?)null);

        _streetcodeCategoryContentRepositoryMock
            .Setup(repository => repository.CreateAsync(
                It.IsAny<StreetcodeCategoryContentEntity>()))
            .Callback<StreetcodeCategoryContentEntity>(
                entity => createdSource = entity)
            .ReturnsAsync((StreetcodeCategoryContentEntity entity) => entity);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock
            .Setup(mapper => mapper.Map<StreetcodeCategoryContentDTO>(
                It.IsAny<object>()))
            .Returns(expectedDto);

        var handler = new CreateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageProcessorMock.Object,
            _blobServiceMock.Object);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        Assert.NotNull(createdSource);
        Assert.Equal(sourceDto.Text, createdSource.Text);
        Assert.Equal(sourceDto.StreetcodeId, createdSource.StreetcodeId);
        Assert.Equal(category.Id, createdSource.SourceLinkCategoryId);

        _streetcodeCategoryContentRepositoryMock.Verify(
            repository => repository.CreateAsync(createdSource),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenNewCategoryTitleAlreadyExists_ShouldReturnFailure()
    {
        const string duplicateTitle = "Documents";
        const string expectedErrorMessage =
            "Source category with this title already exists.";
        var sourceDto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: $" {duplicateTitle} ",
            NewCategoryImage: new ImageFileBaseCreateDTO());
        var command = new CreateSourceCommand(sourceDto);
        var streetcode = new StreetcodeEntity
        {
            Id = sourceDto.StreetcodeId,
        };
        var existingCategory = new SourceLinkCategoryEntity
        {
            Id = 10,
            Title = duplicateTitle,
        };

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        _sourceCategoryRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceLinkCategoryEntity, bool>>>(),
                null))
            .ReturnsAsync(existingCategory);

        var handler = new CreateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageProcessorMock.Object,
            _blobServiceMock.Object);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);

        _loggerMock.Verify(
            logger => logger.LogError(command, expectedErrorMessage),
            Times.Once());
        _imageProcessorMock.VerifyNoOtherCalls();
        _blobServiceMock.VerifyNoOtherCalls();
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenNewCategoryIsValid_ShouldCreateSourceWithCategoryAndImage()
    {
        const string categoryTitle = "Documents";
        var inputImage = new ImageFileBaseCreateDTO
        {
            Title = "Documents image",
            BaseFormat = "original-base64",
            MimeType = "image/png",
            Extension = "png",
        };
        var grayscaleImage = new ImageFileBaseCreateDTO
        {
            Title = inputImage.Title,
            BaseFormat = "AQID",
            MimeType = inputImage.MimeType,
            Extension = inputImage.Extension,
        };
        string expectedImageHash = Convert.ToHexString(
            SHA256.HashData(
                Convert.FromBase64String(grayscaleImage.BaseFormat)));
        var sourceDto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: $" {categoryTitle} ",
            NewCategoryImage: inputImage);
        var command = new CreateSourceCommand(sourceDto);
        var streetcode = new StreetcodeEntity
        {
            Id = sourceDto.StreetcodeId,
        };
        var imageEntity = new ImageEntity
        {
            MimeType = grayscaleImage.MimeType,
        };
        var expectedDto = new StreetcodeCategoryContentDTO
        {
            Text = sourceDto.Text,
            StreetcodeId = sourceDto.StreetcodeId,
        };
        StreetcodeCategoryContentEntity? createdSource = null;

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        _sourceCategoryRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceLinkCategoryEntity, bool>>>(),
                null))
            .ReturnsAsync((SourceLinkCategoryEntity?)null);

        _imageProcessorMock
            .Setup(processor => processor.ConvertToGrayscale(inputImage))
            .Returns(grayscaleImage);

        _blobServiceMock
            .Setup(service => service.SaveFileInStorage(
                grayscaleImage.BaseFormat!,
                categoryTitle,
                grayscaleImage.Extension!))
            .Returns("grayscale-image");

        _mapperMock
            .Setup(mapper => mapper.Map<ImageEntity>(grayscaleImage))
            .Returns(imageEntity);

        _streetcodeCategoryContentRepositoryMock
            .Setup(repository => repository.CreateAsync(
                It.IsAny<StreetcodeCategoryContentEntity>()))
            .Callback<StreetcodeCategoryContentEntity>(
                entity => createdSource = entity)
            .ReturnsAsync((StreetcodeCategoryContentEntity entity) => entity);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock
            .Setup(mapper => mapper.Map<StreetcodeCategoryContentDTO>(
                It.IsAny<object>()))
            .Returns(expectedDto);

        var handler = new CreateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageProcessorMock.Object,
            _blobServiceMock.Object);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        Assert.NotNull(createdSource);
        Assert.Equal(sourceDto.Text, createdSource.Text);
        Assert.Equal(sourceDto.StreetcodeId, createdSource.StreetcodeId);
        Assert.NotNull(createdSource.SourceLinkCategory);
        Assert.Equal(categoryTitle, createdSource.SourceLinkCategory.Title);
        Assert.Equal(
            expectedImageHash,
            createdSource.SourceLinkCategory.ImageHash);
        Assert.Same(imageEntity, createdSource.SourceLinkCategory.Image);
        Assert.Equal("grayscale-image.png", imageEntity.BlobName);

        _imageProcessorMock.Verify(
            processor => processor.ConvertToGrayscale(inputImage),
            Times.Once());
        _blobServiceMock.Verify(
            service => service.SaveFileInStorage(
                grayscaleImage.BaseFormat!,
                categoryTitle,
                grayscaleImage.Extension!),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenNewCategoryImageAlreadyExists_ShouldReturnFailure()
    {
        const string expectedErrorMessage =
            "Source category with this image already exists.";
        var inputImage = new ImageFileBaseCreateDTO
        {
            BaseFormat = "original-base64",
            MimeType = "image/png",
            Extension = "png",
        };
        var grayscaleImage = new ImageFileBaseCreateDTO
        {
            BaseFormat = "AQID",
            MimeType = inputImage.MimeType,
            Extension = inputImage.Extension,
        };
        string imageHash = Convert.ToHexString(
            SHA256.HashData(
                Convert.FromBase64String(grayscaleImage.BaseFormat)));
        var sourceDto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: "Documents",
            NewCategoryImage: inputImage);
        var command = new CreateSourceCommand(sourceDto);
        var existingCategory = new SourceLinkCategoryEntity
        {
            Id = 10,
            ImageHash = imageHash,
        };

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(new StreetcodeEntity
            {
                Id = sourceDto.StreetcodeId,
            });

        _sourceCategoryRepositoryMock
            .SetupSequence(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceLinkCategoryEntity, bool>>>(),
                null))
            .ReturnsAsync((SourceLinkCategoryEntity?)null)
            .ReturnsAsync(existingCategory);

        _imageProcessorMock
            .Setup(processor => processor.ConvertToGrayscale(inputImage))
            .Returns(grayscaleImage);

        var handler = new CreateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageProcessorMock.Object,
            _blobServiceMock.Object);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);
        _blobServiceMock.VerifyNoOtherCalls();
        _streetcodeCategoryContentRepositoryMock.Verify(
            repository => repository.CreateAsync(
                It.IsAny<StreetcodeCategoryContentEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedErrorMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenNewCategorySaveFails_ShouldDeleteCreatedBlob()
    {
        const string categoryTitle = "Documents";
        const string blobName = "grayscale-image.png";
        const string expectedErrorMessage = "Failed to create source block.";
        var inputImage = new ImageFileBaseCreateDTO
        {
            BaseFormat = "original-base64",
            MimeType = "image/png",
            Extension = "png",
        };
        var grayscaleImage = new ImageFileBaseCreateDTO
        {
            BaseFormat = "AQID",
            MimeType = inputImage.MimeType,
            Extension = inputImage.Extension,
        };
        var sourceDto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: categoryTitle,
            NewCategoryImage: inputImage);
        var command = new CreateSourceCommand(sourceDto);

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(new StreetcodeEntity
            {
                Id = sourceDto.StreetcodeId,
            });

        _sourceCategoryRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceLinkCategoryEntity, bool>>>(),
                null))
            .ReturnsAsync((SourceLinkCategoryEntity?)null);

        _imageProcessorMock
            .Setup(processor => processor.ConvertToGrayscale(inputImage))
            .Returns(grayscaleImage);

        _blobServiceMock
            .Setup(service => service.SaveFileInStorage(
                grayscaleImage.BaseFormat!,
                categoryTitle,
                grayscaleImage.Extension!))
            .Returns("grayscale-image");

        _mapperMock
            .Setup(mapper => mapper.Map<ImageEntity>(grayscaleImage))
            .Returns(new ImageEntity
            {
                MimeType = grayscaleImage.MimeType,
            });

        _streetcodeCategoryContentRepositoryMock
            .Setup(repository => repository.CreateAsync(
                It.IsAny<StreetcodeCategoryContentEntity>()))
            .ReturnsAsync((StreetcodeCategoryContentEntity entity) => entity);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var handler = new CreateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageProcessorMock.Object,
            _blobServiceMock.Object);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);

        _blobServiceMock.Verify(
            service => service.DeleteFileInStorage(blobName),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedErrorMessage),
            Times.Once());
    }
}
