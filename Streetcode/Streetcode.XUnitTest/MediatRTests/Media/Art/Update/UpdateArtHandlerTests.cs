using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.Update;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.XUnitTest.MediatRTests.Media.Art.Update;

public class UpdateArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IArtRepository> _artRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly UpdateArtHandler _handler;

    public UpdateArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ArtRepository)
            .Returns(_artRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);

        _handler = new UpdateArtHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenArtDoesNotExist_ShouldReturnFailure()
    {
        var command = new UpdateArtCommand(1, CreateArtDto());
        var expectedError = $"Cannot find art with id: {command.Id}";

        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtIdLookup(predicate, command.Id)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync((ArtEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var command = new UpdateArtCommand(1, CreateArtDto());
        var expectedError = $"Cannot find image with id: {command.Art.ImageId}";

        SetupArtExists(command, CreateArtEntity());
        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ImageEntity, bool>>>(
                    predicate => MatchesImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ArtEntity>()),
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
        var command = new UpdateArtCommand(1, CreateArtDto());
        var expectedError = $"Image with id: {command.Art.ImageId} is already used by another art";
        var art = CreateArtEntity();
        var otherArt = new ArtEntity { Id = art.Id + 1, ImageId = command.Art.ImageId };

        SetupArtExists(command, art);
        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(otherArt);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenImageBelongsToSameArt_ShouldSucceed()
    {
        var art = CreateArtEntity();
        var command = new UpdateArtCommand(art.Id, CreateArtDto());
        var expectedDto = new ArtDTO { Id = art.Id };

        SetupArtExists(command, art);
        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(art);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<ArtDTO>(art))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _artRepositoryMock.Verify(
            repository => repository.Update(art),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var art = CreateArtEntity();
        var command = new UpdateArtCommand(art.Id, CreateArtDto());
        var expectedError = $"Failed to update art with id: {command.Id}";

        SetupArtExists(command, art);
        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync((ArtEntity)null!);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _mapperMock.Verify(
            mapper => mapper.Map<ArtDTO>(It.IsAny<ArtEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenUpdateSucceeds_ShouldTrimTextAndReturnUpdatedArt()
    {
        var art = CreateArtEntity();
        var inputDto = CreateArtDto();
        inputDto.Title = "  Padded title  ";
        inputDto.Description = "  Padded description  ";
        var command = new UpdateArtCommand(art.Id, inputDto);
        var expectedDto = new ArtDTO { Id = art.Id };

        SetupArtExists(command, art);
        SetupImageExists(command);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync((ArtEntity)null!);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<ArtDTO>(art))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        Assert.Equal("Padded title", art.Title);
        Assert.Equal("Padded description", art.Description);
        Assert.Equal(command.Art.ImageId, art.ImageId);
        _artRepositoryMock.Verify(
            repository => repository.Update(art),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    private static bool MatchesArtIdLookup(
        Expression<Func<ArtEntity, bool>> predicate,
        int id)
    {
        var compiled = predicate.Compile();
        var matching = new ArtEntity { Id = id };
        var other = new ArtEntity { Id = id + 1 };
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

    private static bool MatchesImageLookup(
        Expression<Func<ImageEntity, bool>> predicate,
        int imageId)
    {
        var compiled = predicate.Compile();
        var matching = new ImageEntity { Id = imageId };
        var other = new ImageEntity { Id = imageId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private void SetupArtExists(UpdateArtCommand command, ArtEntity art)
    {
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtIdLookup(predicate, command.Id)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(art);
    }

    private void SetupImageExists(UpdateArtCommand command)
    {
        var image = new ImageEntity { Id = command.Art.ImageId };

        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ImageEntity, bool>>>(
                    predicate => MatchesImageLookup(predicate, command.Art.ImageId)),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
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
