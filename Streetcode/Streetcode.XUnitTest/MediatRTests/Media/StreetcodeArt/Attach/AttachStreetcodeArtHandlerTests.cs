using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;
using StreetcodeArtEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeArt;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

namespace Streetcode.XUnitTest.MediatRTests.Media.StreetcodeArt.Attach;

public class AttachStreetcodeArtHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeArtRepository> _streetcodeArtRepositoryMock = new();
    private readonly Mock<IArtRepository> _artRepositoryMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly AttachStreetcodeArtHandler _handler;

    public AttachStreetcodeArtHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeArtRepository)
            .Returns(_streetcodeArtRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ArtRepository)
            .Returns(_artRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _handler = new AttachStreetcodeArtHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
    {
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var expectedError = $"Cannot find streetcode with id: {command.Attach.StreetcodeId}";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeEntity, bool>>>(
                    predicate => MatchesStreetcodeLookup(predicate, command.Attach.StreetcodeId)),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, IIncludableQueryable<StreetcodeEntity, object>>?>()))
            .ReturnsAsync((StreetcodeEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<StreetcodeArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenArtDoesNotExist_ShouldReturnFailure()
    {
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var expectedError = $"Cannot find art with id: {command.Attach.ArtId}";

        SetupStreetcodeExists(command.Attach.StreetcodeId);
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtIdLookup(predicate, command.Attach.ArtId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync((ArtEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<StreetcodeArtEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenAlreadyAttached_ShouldReturnFailure()
    {
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var expectedError =
            $"Art with id: {command.Attach.ArtId} is already attached to streetcode with id: {command.Attach.StreetcodeId}";

        SetupStreetcodeExists(command.Attach.StreetcodeId);
        SetupArtExists(command.Attach.ArtId);
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesStreetcodeArtLookup(
                        predicate,
                        command.Attach.StreetcodeId,
                        command.Attach.ArtId)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(new StreetcodeArtEntity
            {
                StreetcodeId = command.Attach.StreetcodeId,
                ArtId = command.Attach.ArtId,
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<StreetcodeArtEntity>()),
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
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var expectedError =
            $"Failed to attach art with id: {command.Attach.ArtId} to streetcode with id: {command.Attach.StreetcodeId}";

        SetupSuccessfulPreconditions(command, existingLinks: new List<StreetcodeArtEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _mapperMock.Verify(
            mapper => mapper.Map<StreetcodeArtDTO>(It.IsAny<StreetcodeArtEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenNoExistingLinks_ShouldAssignIndexOneAndSucceed()
    {
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var expectedDto = new StreetcodeArtDTO { StreetcodeId = command.Attach.StreetcodeId, Index = 1 };

        SetupSuccessfulPreconditions(command, existingLinks: new List<StreetcodeArtEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<StreetcodeArtDTO>(It.Is<StreetcodeArtEntity>(link => link.Index == 1)))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.CreateAsync(It.Is<StreetcodeArtEntity>(link => link.Index == 1)),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenExistingLinksPresent_ShouldAssignNextIndex()
    {
        var command = new AttachStreetcodeArtCommand(CreateAttachDto());
        var existingLinks = new List<StreetcodeArtEntity>
        {
            new() { StreetcodeId = command.Attach.StreetcodeId, ArtId = 1, Index = 1 },
            new() { StreetcodeId = command.Attach.StreetcodeId, ArtId = 2, Index = 3 },
        };
        var expectedDto = new StreetcodeArtDTO { StreetcodeId = command.Attach.StreetcodeId, Index = 4 };

        SetupSuccessfulPreconditions(command, existingLinks);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<StreetcodeArtDTO>(It.Is<StreetcodeArtEntity>(link => link.Index == 4)))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _streetcodeArtRepositoryMock.Verify(
            repository => repository.CreateAsync(It.Is<StreetcodeArtEntity>(link => link.Index == 4)),
            Times.Once());
    }

    private static bool MatchesStreetcodeLookup(
        Expression<Func<StreetcodeEntity, bool>> predicate,
        int streetcodeId)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeEntity { Id = streetcodeId };
        var other = new StreetcodeEntity { Id = streetcodeId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private static bool MatchesArtIdLookup(
        Expression<Func<ArtEntity, bool>> predicate,
        int artId)
    {
        var compiled = predicate.Compile();
        var matching = new ArtEntity { Id = artId };
        var other = new ArtEntity { Id = artId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private static bool MatchesStreetcodeArtLookup(
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

    private static bool MatchesStreetcodeArtByStreetcodeLookup(
        Expression<Func<StreetcodeArtEntity, bool>> predicate,
        int streetcodeId)
    {
        var compiled = predicate.Compile();
        var matching = new StreetcodeArtEntity { StreetcodeId = streetcodeId };
        var other = new StreetcodeArtEntity { StreetcodeId = streetcodeId + 1 };
        return compiled(matching) && !compiled(other);
    }

    private void SetupStreetcodeExists(int streetcodeId)
    {
        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeEntity, bool>>>(
                    predicate => MatchesStreetcodeLookup(predicate, streetcodeId)),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, IIncludableQueryable<StreetcodeEntity, object>>?>()))
            .ReturnsAsync(new StreetcodeEntity { Id = streetcodeId });
    }

    private void SetupArtExists(int artId)
    {
        _artRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<ArtEntity, bool>>>(
                    predicate => MatchesArtIdLookup(predicate, artId)),
                It.IsAny<Func<IQueryable<ArtEntity>, IIncludableQueryable<ArtEntity, object>>?>()))
            .ReturnsAsync(new ArtEntity { Id = artId });
    }

    private void SetupSuccessfulPreconditions(
        AttachStreetcodeArtCommand command,
        List<StreetcodeArtEntity> existingLinks)
    {
        SetupStreetcodeExists(command.Attach.StreetcodeId);
        SetupArtExists(command.Attach.ArtId);
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesStreetcodeArtLookup(
                        predicate,
                        command.Attach.StreetcodeId,
                        command.Attach.ArtId)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync((StreetcodeArtEntity)null!);
        _streetcodeArtRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.Is<Expression<Func<StreetcodeArtEntity, bool>>>(
                    predicate => MatchesStreetcodeArtByStreetcodeLookup(predicate, command.Attach.StreetcodeId)),
                It.IsAny<Func<IQueryable<StreetcodeArtEntity>, IIncludableQueryable<StreetcodeArtEntity, object>>?>()))
            .ReturnsAsync(existingLinks);
    }

    private static StreetcodeArtAttachDto CreateAttachDto()
    {
        return new StreetcodeArtAttachDto
        {
            StreetcodeId = 10,
            ArtId = 5,
        };
    }
}
