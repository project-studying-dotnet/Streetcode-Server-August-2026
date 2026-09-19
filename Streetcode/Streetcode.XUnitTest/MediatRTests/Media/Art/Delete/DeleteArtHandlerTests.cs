using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;
using StreetcodeArtEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeArt;

namespace Streetcode.XUnitTest.MediatRTests.Media.Art.Delete;

public class DeleteArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IArtRepository> _artRepositoryMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly DeleteArtHandler _handler;

    public DeleteArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ArtRepository)
            .Returns(_artRepositoryMock.Object);

        _handler = new DeleteArtHandler(
            _repositoryWrapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenArtDoesNotExist_ShouldReturnFailure()
    {
        var command = new DeleteArtCommand(1);
        var expectedError = $"Cannot find art with id: {command.Id}";

        SetupArtLookup(command.Id, art: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<ArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenArtIsPublishedForStreetcode_ShouldReturnFailure()
    {
        var command = new DeleteArtCommand(1);
        var expectedError =
            $"Cannot delete art with id: {command.Id}, because it is already published for a streetcode";
        var art = CreateArtEntity();
        art.StreetcodeArts.Add(new StreetcodeArtEntity { ArtId = art.Id, StreetcodeId = 10 });

        SetupArtLookup(command.Id, art);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<ArtEntity>()),
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
        var command = new DeleteArtCommand(1);
        var art = CreateArtEntity();
        var expectedError = $"Failed to delete art with id: {command.Id}";

        SetupArtLookup(command.Id, art);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _artRepositoryMock.Verify(
            repository => repository.Delete(art),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenArtIsUnpublished_ShouldDeleteAndReturnSuccess()
    {
        var command = new DeleteArtCommand(1);
        var art = CreateArtEntity();

        SetupArtLookup(command.Id, art);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        _artRepositoryMock.Verify(
            repository => repository.Delete(art),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
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

    private void SetupArtLookup(int id, ArtEntity? art)
    {
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtIdLookup(predicate, id)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(art!);
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
