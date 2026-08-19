using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.Update;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;
using ImageDetailsEntity = Streetcode.DAL.Entities.Media.Images.ImageDetails;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class UpdateFactHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IImageDetailsRepository> _imageDetailsRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public UpdateFactHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageDetailsRepository)
            .Returns(_imageDetailsRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailure()
    {
        var factDto = CreateFactDto();
        var command = new UpdateFactCommand(15, factDto);
        const string expectedMessage = "Cannot find fact with id: 15";

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync((FactEntity?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _imageRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenStreetcodeIdChanges_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(streetcodeId: 20);
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = 10 };
        const string expectedMessage = "Cannot move fact with id 15 to another streetcode";

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _imageRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var factDto = CreateFactDto();
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        const string expectedMessage = "Cannot find image with id: 5";

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _mapperMock.VerifyNoOtherCalls();
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(title: "  Updated fact  ", factContent: "  Updated content  ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        const string expectedMessage = "Failed to update fact with id: 15";

        SetupValidDependencies(factDto, fact, image);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        Assert.Equal("Updated fact", fact.Title);
        Assert.Equal("Updated content", fact.FactContent);

        _factRepositoryMock.Verify(repository => repository.Update(fact), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDescriptionProvidedAndDetailsDoNotExist_ShouldCreateImageDetails()
    {
        var factDto = CreateFactDto(imageAlt: "  New description  ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var resultDto = new FactDto { Id = command.Id };
        ImageDetailsEntity? createdImageDetails = null;

        SetupValidDependencies(factDto, fact, image);
        _imageDetailsRepositoryMock
            .Setup(repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()))
            .Callback<ImageDetailsEntity>(details => createdImageDetails = details)
            .Returns((ImageDetailsEntity details) => Task.FromResult(details));
        SetupSuccessfulSave(fact, resultDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(resultDto, result.Value);
        Assert.NotNull(createdImageDetails);
        Assert.Same(createdImageDetails, image.ImageDetails);
        Assert.Equal("New description", createdImageDetails.Alt);
        Assert.Equal("New description", result.Value.ImageAlt);

        _imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(createdImageDetails),
            Times.Once());
        _imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageAltIsWhitespaceAndDetailsExist_ShouldClearAlt()
    {
        var factDto = CreateFactDto(imageAlt: "   ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Old description" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var resultDto = new FactDto { Id = command.Id };

        SetupValidDependencies(factDto, fact, image);
        SetupSuccessfulSave(fact, resultDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(imageDetails.Alt);
        Assert.Null(result.Value.ImageAlt);

        _imageDetailsRepositoryMock.Verify(repository => repository.Update(imageDetails), Times.Once());
        _imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
        _loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageAltIsNull_ShouldNotTouchImageDetails()
    {
        var factDto = CreateFactDto(imageAlt: null);
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Existing alt" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var resultDto = new FactDto { Id = command.Id };

        SetupValidDependencies(factDto, fact, image);
        SetupSuccessfulSave(fact, resultDto);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Existing alt", imageDetails.Alt);

        _imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
        _imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    private UpdateFactHandler CreateHandler() =>
        new(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

    private void SetupValidDependencies(
        FactUpdateCreateDto factDto,
        FactEntity fact,
        ImageEntity image)
    {
        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
        _mapperMock
            .Setup(mapper => mapper.Map(factDto, fact))
            .Returns(fact);
    }

    private void SetupSuccessfulSave(FactEntity fact, FactDto resultDto)
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(resultDto);
    }

    private static FactUpdateCreateDto CreateFactDto(
        string title = "Updated fact",
        string factContent = "Updated content",
        string? imageAlt = null,
        int streetcodeId = 10) =>
        new()
        {
            Title = title,
            FactContent = factContent,
            ImageAlt = imageAlt,
            ImageId = 5,
            StreetcodeId = streetcodeId,
        };
}
