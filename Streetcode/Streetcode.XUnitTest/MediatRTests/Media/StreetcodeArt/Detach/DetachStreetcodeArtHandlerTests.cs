using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Xunit;
using StreetcodeArtEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeArt;

namespace Streetcode.XUnitTest.MediatRTests.Media.StreetcodeArt.Detach;

public class DetachStreetcodeArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeArtRepository> _streetcodeArtRepositoryMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly DetachStreetcodeArtHandler _handler;

    public DetachStreetcodeArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeArtRepository)
            .Returns(_streetcodeArtRepositoryMock.Object);

        _handler = new DetachStreetcodeArtHandler(
            _repositoryWrapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenLinkDoesNotExist_ShouldReturnFailure()
    {
        var command = new DetachStreetcodeArtCommand(10, 5);
        var expectedError =
            $"Cannot find art with id: {command.ArtId} attached to streetcode with id: {command.StreetcodeId}";

        SetupLinkLookup(command, link: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<StreetcodeArtEntity>()),
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
        var command = new DetachStreetcodeArtCommand(10, 5);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 2 };
        var expectedError =
            $"Failed to detach art with id: {command.ArtId} from streetcode with id: {command.StreetcodeId}";

        SetupLinkLookup(command, link);
        SetupFollowingLinks(command, link.Index, new List<StreetcodeArtEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.Delete(link),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenNoFollowingLinks_ShouldDeleteAndReturnSuccess()
    {
        var command = new DetachStreetcodeArtCommand(10, 5);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 2 };

        SetupLinkLookup(command, link);
        SetupFollowingLinks(command, link.Index, new List<StreetcodeArtEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.Delete(link),
            Times.Once());
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.UpdateRange(It.Is<IEnumerable<StreetcodeArtEntity>>(links => !links.Any())),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenFollowingLinksExist_ShouldShiftIndexesDownAndSucceed()
    {
        var command = new DetachStreetcodeArtCommand(10, 5);
        var link = new StreetcodeArtEntity { StreetcodeId = 10, ArtId = 5, Index = 2 };
        var following = new List<StreetcodeArtEntity>
        {
            new() { StreetcodeId = 10, ArtId = 6, Index = 3 },
            new() { StreetcodeId = 10, ArtId = 7, Index = 4 },
        };

        SetupLinkLookup(command, link);
        SetupFollowingLinks(command, link.Index, following);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, following[0].Index);
        Assert.Equal(3, following[1].Index);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.UpdateRange(following),
            Times.Once());
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.Delete(link),
            Times.Once());
    }

    private static bool MatchesLinkLookup(
        Expression<Func<StreetcodeArtEntity, bool>> predicate,
        int streetcodeId,
        int artId)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeArtEntity { StreetcodeId = streetcodeId, ArtId = artId };
        var otherArt = new StreetcodeArtEntity { StreetcodeId = streetcodeId, ArtId = artId + 1 };
        var otherStreetcode = new StreetcodeArtEntity { StreetcodeId = streetcodeId + 1, ArtId = artId };
        return compiled(matching) && !compiled(otherArt) && !compiled(otherStreetcode);
    }

    private void SetupLinkLookup(DetachStreetcodeArtCommand command, StreetcodeArtEntity? link)
    {
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesLinkLookup(predicate, command.StreetcodeId, command.ArtId)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(link!);
    }

    private static bool MatchesFollowingLinksLookup(
        Expression<Func<StreetcodeArtEntity, bool>> predicate,
        int streetcodeId,
        int afterIndex)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeArtEntity { StreetcodeId = streetcodeId, Index = afterIndex + 1 };
        var wrongStreetcode = new StreetcodeArtEntity { StreetcodeId = streetcodeId + 1, Index = afterIndex + 1 };
        var notAfter = new StreetcodeArtEntity { StreetcodeId = streetcodeId, Index = afterIndex };
        return compiled(matching) && !compiled(wrongStreetcode) && !compiled(notAfter);
    }

    private void SetupFollowingLinks(
        DetachStreetcodeArtCommand command,
        int afterIndex,
        List<StreetcodeArtEntity> followingLinks)
    {
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesFollowingLinksLookup(predicate, command.StreetcodeId, afterIndex)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(followingLinks);
    }
}
