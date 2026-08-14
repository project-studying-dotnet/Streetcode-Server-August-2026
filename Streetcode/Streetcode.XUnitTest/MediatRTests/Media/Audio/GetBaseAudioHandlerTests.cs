using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.GetBaseAudio;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio;

public class GetBaseAudioHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public GetBaseAudioHandlerTests()
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
        var query = new GetBaseAudioQuery(5);
        var expectedError = $"Cannot find an audio with corresponding id: {query.Id}";
        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync((AudioEntity?)null);

        var handler = new GetBaseAudioHandler(
            _blobServiceMock.Object,
            _repositoryWrapperMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0].Message);

        _audioRepositoryMock.Verify(repo => repo.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<
                IQueryable<AudioEntity>,
                IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _loggerMock.Verify(logger => logger.LogError(query, expectedError), Times.Once());
        _blobServiceMock.Verify(blob => blob.FindFileInStorageAsMemoryStream(
            It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioExists_ShouldReturnSuccess()
    {
        var query = new GetBaseAudioQuery(5);
        var blobName = "audio.mp3";

        var audioEntity = new AudioEntity
        {
            Id = query.Id,
            BlobName = blobName,
        };

        using var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<IQueryable<AudioEntity>, IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audioEntity);
        _blobServiceMock
            .Setup(blob => blob.FindFileInStorageAsMemoryStream(blobName))
            .Returns(expectedStream);

        var handler = new GetBaseAudioHandler(
            _blobServiceMock.Object,
            _repositoryWrapperMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedStream, result.Value);

        _audioRepositoryMock
            .Verify(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()),
                Times.Once());
        _blobServiceMock
            .Verify(blob => blob.FindFileInStorageAsMemoryStream(blobName),
                Times.Once());

        _loggerMock.Verify(logger => logger.LogError(
            It.IsAny<object>(),
            It.IsAny<string>()),
            Times.Never());
    }
}
