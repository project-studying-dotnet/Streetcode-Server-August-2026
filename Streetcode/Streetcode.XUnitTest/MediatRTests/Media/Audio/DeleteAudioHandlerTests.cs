using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio;

public class DeleteAudioHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public DeleteAudioHandlerTests()
    {
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
        _audioRepositoryMock = new Mock<IAudioRepository>();
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILoggerService>();
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.AudioRepository)
            .Returns(_audioRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAudioDoesNotExist_ShouldReturnFailure()
    {
        var command = new DeleteAudioCommand(5);
        var expectedError = string.Format(TestMessages.CannotFindAnAudioWithCorrespondingCategoryId, command.Id);
        _audioRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync((AudioEntity?)null);

        var handler = new DeleteAudioHandler(
            _repositoryWrapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0].Message);

        _audioRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _audioRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<AudioEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
        _blobServiceMock.Verify(
            blob => blob.DeleteFileInStorage(It.IsAny<string>()),
            Times.Never());
        _loggerMock.Verify(logger => logger.LogError(command, expectedError), Times.Once());
        _loggerMock.Verify(
            logger => logger.LogInformation(It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioIsDeleted_ShouldReturnSuccess()
    {
        var command = new DeleteAudioCommand(5);
        var audioEntity = new AudioEntity
        {
            Id = command.Id,
            BlobName = "audio.mp3",
            MimeType = "audio/mpeg",
        };
        const string expectedLogMessage = "DeleteAudioCommand handled successfully";
        _audioRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audioEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var handler = new DeleteAudioHandler(
            _repositoryWrapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);

        _audioRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _audioRepositoryMock.Verify(repository => repository.Delete(audioEntity), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _blobServiceMock.Verify(
            blob => blob.DeleteFileInStorage(audioEntity.BlobName),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogInformation(expectedLogMessage),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioIsNotDeleted_ShouldReturnFailure()
    {
        var command = new DeleteAudioCommand(5);
        var audioEntity = new AudioEntity
        {
            Id = command.Id,
            BlobName = "audio.mp3",
            MimeType = "audio/mpeg",
        };
        var expectedError = TestMessages.FailedToDeleteAnAudio;
        _audioRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audioEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var handler = new DeleteAudioHandler(
            _repositoryWrapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0].Message);

        _audioRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _audioRepositoryMock.Verify(repository => repository.Delete(audioEntity), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _blobServiceMock.Verify(
            blob => blob.DeleteFileInStorage(It.IsAny<string>()),
            Times.Never());
        _loggerMock.Verify(logger => logger.LogError(command, expectedError), Times.Once());
        _loggerMock.Verify(
            logger => logger.LogInformation(It.IsAny<string>()),
            Times.Never());
    }
}
