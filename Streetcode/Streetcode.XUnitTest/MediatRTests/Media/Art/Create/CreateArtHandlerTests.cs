using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.XUnitTest.MediatRTests.Media.Art.Create;

public class CreateArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IArtRepository> _artRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly CreateArtHandler _handler;

    public CreateArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ArtRepository)
            .Returns(_artRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);

        _handler = new CreateArtHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var command = new CreateArtCommand(CreateArtDto());
        var expectedError = $"Cannot find image with id: {command.Art.ImageId}";

        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ImageEntity, bool>>>(
                    predicate => MatchesImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenImageAlreadyUsedByAnotherArt_ShouldReturnFailure()
    {
        var command = new CreateArtCommand(CreateArtDto());
        var expectedError = $"Image with id: {command.Art.ImageId} is already used by another art";

        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(CreateArtEntity());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = new CreateArtCommand(CreateArtDto());
        var artEntity = CreateArtEntity();
        const string expectedError = "Failed to create art";

        SetupCreation(command, artEntity, saveChangesResult: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.CreateAsync(artEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<ArtDTO>(It.IsAny<ArtEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenCreationSucceeds_ShouldReturnCreatedArtAndTrimText()
    {
        var inputDto = CreateArtDto();
        inputDto.Title = "  Padded title  ";
        inputDto.Description = "  Padded description  ";
        var command = new CreateArtCommand(inputDto);
        var artEntity = CreateArtEntity();
        var expectedDto = new ArtDTO
        {
            Id = 1,
            Title = "Padded title",
            Description = "Padded description",
            ImageId = inputDto.ImageId,
        };

        SetupCreation(command, artEntity, saveChangesResult: 1);
        _mapperMock
            .Setup(mapper => mapper.Map<ArtDTO>(artEntity))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        Assert.Equal("Padded title", artEntity.Title);
        Assert.Equal("Padded description", artEntity.Description);
        _artRepositoryMock.Verify(
            repository => repository.CreateAsync(artEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<ArtDTO>(artEntity),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    private static bool MatchesImageLookup(
        Expression<Func<ImageEntity, bool>> predicate,
        int imageId)
    {
        var compiled = predicate.Compile();
        var matching = new ImageEntity { Id = imageId };
        var other = new ImageEntity { Id = imageId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private static bool MatchesArtImageLookup(
        Expression<Func<ArtEntity, bool>> predicate,
        int imageId)
    {
        var compiled = predicate.Compile();
        var matching = new ArtEntity { ImageId = imageId };
        var other = new ArtEntity { ImageId = imageId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private void SetupImageExists(CreateArtCommand command)
    {
        var image = new ImageEntity { Id = command.Art.ImageId };

        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ImageEntity, bool>>>(
                    predicate => MatchesImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
    }

    private void SetupCreation(
        CreateArtCommand command,
        ArtEntity artEntity,
        int saveChangesResult)
    {
        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync((ArtEntity)null!);
        _mapperMock
            .Setup(mapper => mapper.Map<ArtEntity>(command.Art))
            .Returns(artEntity);
        _artRepositoryMock
            .Setup(repository => repository.CreateAsync(artEntity))
            .ReturnsAsync(artEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(saveChangesResult);
    }

    private static ArtUpdateCreateDto CreateArtDto()
    {
        return new ArtUpdateCreateDto
        {
            ImageId = 5,
            Title = "Test title",
            Description = "Test description",
        };
    }

    private static ArtEntity CreateArtEntity()
    {
        return new ArtEntity
        {
            Id = 1,
            ImageId = 5,
            Title = "Test title",
            Description = "Test description",
        };
    }
}
