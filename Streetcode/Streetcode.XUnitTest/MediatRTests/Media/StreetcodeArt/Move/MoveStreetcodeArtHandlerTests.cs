using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Enums;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Xunit;
using StreetcodeArtEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeArt;

namespace Streetcode.XUnitTest.MediatRTests.Media.StreetcodeArt.Move;

public class MoveStreetcodeArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeArtRepository> _streetcodeArtRepositoryMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly MoveStreetcodeArtHandler _handler;

    public MoveStreetcodeArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeArtRepository)
            .Returns(_streetcodeArtRepositoryMock.Object);

        _handler = new MoveStreetcodeArtHandler(
            _repositoryWrapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenLinkDoesNotExist_ShouldReturnFailure()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Forward);
        var expectedError =
            $"Cannot find art with id: {command.ArtId} attached to streetcode with id: {command.StreetcodeId}";

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenMovingForwardFromLastPosition_ShouldReturnFailure()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Forward);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 3 };
        var expectedError = $"Cannot move art with id: {command.ArtId}, because it is already at the last position";

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link);
        SetupNeighborLookup(command.StreetcodeId, targetIndex: 4, neighbor: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenMovingBackwardFromFirstPosition_ShouldReturnFailure()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Backward);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 1 };
        var expectedError = $"Cannot move art with id: {command.ArtId}, because it is already at the first position";

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link);
        SetupNeighborLookup(command.StreetcodeId, targetIndex: 0, neighbor: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Forward);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 1 };
        var neighbor = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 6, Index = 2 };
        var expectedError =
            $"Failed to move art with id: {command.ArtId} for streetcode with id: {command.StreetcodeId}";

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link);
        SetupNeighborLookup(command.StreetcodeId, targetIndex: 2, neighbor);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenMovingForward_ShouldSwapIndexesWithNextItem()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Forward);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 1 };
        var neighbor = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 6, Index = 2 };

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link);
        SetupNeighborLookup(command.StreetcodeId, targetIndex: 2, neighbor);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        Assert.Equal(2, link.Index);
        Assert.Equal(1, neighbor.Index);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.UpdateRange(
                It.Is<IEnumerable<StreetcodeArtEntity>>(links =>
                    links.Contains(link) && links.Contains(neighbor))),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenMovingBackward_ShouldSwapIndexesWithPreviousItem()
    {
        var command = new MoveStreetcodeArtCommand(10, 5, MoveDirection.Backward);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 2 };
        var neighbor = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 6, Index = 1 };

        SetupLinkLookup(command.StreetcodeId, command.ArtId, link);
        SetupNeighborLookup(command.StreetcodeId, targetIndex: 1, neighbor);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, link.Index);
        Assert.Equal(2, neighbor.Index);
    }

    private static bool MatchesLinkLookup(
        Expression<Func<StreetcodeArtEntity, bool>> predicate,
        int streetcodeId,
        int artId)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeArtEntity { StreetcodeId = streetcodeId, ArtId = artId };
        var otherArt = new StreetcodeArtEntity { StreetcodeId = streetcodeId, ArtId = artId + 1 };
        return compiled(matching) && !compiled(otherArt);
    }

    private static bool MatchesIndexLookup(
        Expression<Func<StreetcodeArtEntity, bool>> predicate,
        int streetcodeId,
        int index)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeArtEntity { StreetcodeId = streetcodeId, Index = index };
        var other = new StreetcodeArtEntity { StreetcodeId = streetcodeId, Index = index + 1 };
        return compiled(matching) && !compiled(other);
    }

    private void SetupLinkLookup(int streetcodeId, int artId, StreetcodeArtEntity? link)
    {
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesLinkLookup(predicate, streetcodeId, artId)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(link!);
    }

    private void SetupNeighborLookup(int streetcodeId, int targetIndex, StreetcodeArtEntity? neighbor)
    {
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesIndexLookup(predicate, streetcodeId, targetIndex)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(neighbor!);
    }
}
