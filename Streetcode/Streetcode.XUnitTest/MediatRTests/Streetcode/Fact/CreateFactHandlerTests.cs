using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Xunit;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;
using ImageDetailsEntity = Streetcode.DAL.Entities.Media.Images.ImageDetails;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class CreateFactHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IImageDetailsRepository> _imageDetailsRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public CreateFactHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageDetailsRepository)
            .Returns(_imageDetailsRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
    {
        var factDto = new FactUpdateCreateDto
        {
            Title = "Test fact",
            FactContent = "Test content",
            ImageId = 5,
            StreetcodeId = 10,
        };
        var command = new CreateFactCommand(factDto);
        var expectedMessage = "Cannot find streetcode with id: 10";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync((StreetcodeEntity?)null);

        var handler = new CreateFactHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());

        _imageRepositoryMock.VerifyNoOtherCalls();
        _factRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        _imageDetailsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var factDto = new FactUpdateCreateDto
        {
            Title = "Test fact",
            FactContent = "Test content",
            ImageId = 5,
            StreetcodeId = 10,
        };

        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };

        const string expectedMessage = "Cannot find image with id: 5";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<ImageEntity>,
                    IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity?)null);

        var handler = new CreateFactHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        _factRepositoryMock.VerifyNoOtherCalls();
        _imageDetailsRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(title: "  Test fact  ", factContent: "  Test content  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var fact = new FactEntity();
        const string expectedMessage = "Failed to create fact";

        SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        Assert.Equal(1, fact.DisplayOrder);
        Assert.Equal("Test fact", fact.Title);
        Assert.Equal("Test content", fact.FactContent);

        _factRepositoryMock.Verify(repository => repository.CreateAsync(fact), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDataIsValid_ShouldCreateFactWithNextDisplayOrder()
    {
        var factDto = CreateFactDto();
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var existingFacts = new[]
        {
            new FactEntity { DisplayOrder = 1 },
            new FactEntity { DisplayOrder = 3 },
        };
        var fact = new FactEntity();
        var createdFactDto = new FactDto { Id = 15 };

        SetupValidDependencies(factDto, streetcode, image, fact, existingFacts);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(createdFactDto, result.Value);
        Assert.Equal(4, fact.DisplayOrder);
        Assert.Null(result.Value.ImageDescription);

        _factRepositoryMock.Verify(repository => repository.CreateAsync(fact), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _imageDetailsRepositoryMock.VerifyNoOtherCalls();
        _loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageDescriptionProvidedAndDetailsDoNotExist_ShouldCreateImageDetails()
    {
        var factDto = CreateFactDto(imageDescription: "  Accessible description  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var fact = new FactEntity();
        var createdFactDto = new FactDto();
        ImageDetailsEntity? createdImageDetails = null;

        SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        _imageDetailsRepositoryMock
            .Setup(repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()))
            .Callback<ImageDetailsEntity>(details => createdImageDetails = details)
            .Returns((ImageDetailsEntity details) => Task.FromResult(details));
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdImageDetails);
        Assert.Same(createdImageDetails, image.ImageDetails);
        Assert.Equal(image.Id, createdImageDetails.ImageId);
        Assert.Equal("Accessible description", createdImageDetails.Alt);
        Assert.Equal("Accessible description", result.Value.ImageDescription);

        _imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(createdImageDetails),
            Times.Once());
        _imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageDetailsExist_ShouldUpdateDescription()
    {
        var factDto = CreateFactDto(imageDescription: "  Updated description  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Old description" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var fact = new FactEntity();
        var createdFactDto = new FactDto();

        SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated description", imageDetails.Alt);
        Assert.Equal("Updated description", result.Value.ImageDescription);

        _imageDetailsRepositoryMock.Verify(repository => repository.Update(imageDetails), Times.Once());
        _imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    private CreateFactHandler CreateHandler() =>
        new(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

    private void SetupValidDependencies(
        FactUpdateCreateDto factDto,
        StreetcodeEntity streetcode,
        ImageEntity image,
        FactEntity fact,
        IEnumerable<FactEntity> existingFacts)
    {
        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);
        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(existingFacts);
        _mapperMock
            .Setup(mapper => mapper.Map<FactEntity>(factDto))
            .Returns(fact);
        _factRepositoryMock
            .Setup(repository => repository.CreateAsync(fact))
            .ReturnsAsync(fact);
    }

    private static FactUpdateCreateDto CreateFactDto(
        string title = "Test fact",
        string factContent = "Test content",
        string? imageDescription = null) =>
        new()
        {
            Title = title,
            FactContent = factContent,
            ImageDescription = imageDescription,
            ImageId = 5,
            StreetcodeId = 10,
        };
}
