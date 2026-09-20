using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.GetAll;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio;

public class GetAllAudiosHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<IMapper> _mapperMock;

    public GetAllAudiosHandlerTests()
    {
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
        _audioRepositoryMock = new Mock<IAudioRepository>();
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILoggerService>();
        _mapperMock = new Mock<IMapper>();
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.AudioRepository)
            .Returns(_audioRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAudiosDoNotExist_ShouldReturnFailure()
    {
        var query = new GetAllAudiosQuery();
        var expectedError = TestMessages.CannotFindAnyAudios;

        _audioRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync((IEnumerable<AudioEntity>)null!);

        var handler = new GetAllAudiosHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _audioRepositoryMock.Verify(repo => repo.GetAllAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<
                IQueryable<AudioEntity>,
                IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(query, expectedError),
            Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<IEnumerable<AudioDTO>>(
                It.IsAny<object>()),
            Times.Never());
        _blobServiceMock.Verify(blob => blob.FindFileInStorageAsBase64(
                It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudiosExist_ShouldReturnSuccess()
    {
        const string firstBlobName = "audio-1.mp3";
        const string secondBlobName = "audio-2.mp3";
        const string firstExpectedBase64 = "first-audio-base64";
        const string secondExpectedBase64 = "second-audio-base64";

        var query = new GetAllAudiosQuery();
        var audios = new List<AudioEntity>
        {
            new AudioEntity
            {
                Id = 1,
                BlobName = firstBlobName,
            },
            new AudioEntity
            {
                Id = 2,
                BlobName = secondBlobName,
            },
        };
        var audioDtos = new List<AudioDTO>
        {
            new AudioDTO
            {
                Id = 1,
                BlobName = firstBlobName,
                MimeType = "audio/mpeg",
                Base64 = string.Empty,
            },
            new AudioDTO
            {
                Id = 2,
                BlobName = secondBlobName,
                MimeType = "audio/mpeg",
                Base64 = string.Empty,
            },
        };

        _audioRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audios);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<AudioDTO>>(audios))
            .Returns(audioDtos);
        _blobServiceMock
            .Setup(blob => blob.FindFileInStorageAsBase64(firstBlobName))
            .Returns(firstExpectedBase64);
        _blobServiceMock
            .Setup(blob => blob.FindFileInStorageAsBase64(secondBlobName))
            .Returns(secondExpectedBase64);

        var handler = new GetAllAudiosHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(audioDtos, result.Value);
        Assert.Equal(firstExpectedBase64, audioDtos[0].Base64);
        Assert.Equal(secondExpectedBase64, audioDtos[1].Base64);

        _audioRepositoryMock.Verify(repo => repo.GetAllAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<
                IQueryable<AudioEntity>,
                IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<AudioDTO>>(audios),
            Times.Once());
        _blobServiceMock.Verify(
            blob => blob.FindFileInStorageAsBase64(firstBlobName),
            Times.Once());
        _blobServiceMock.Verify(
            blob => blob.FindFileInStorageAsBase64(secondBlobName),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioCollectionIsEmpty_ShouldReturnEmptySuccess()
    {
        var query = new GetAllAudiosQuery();
        var audios = new List<AudioEntity>();
        var audioDtos = new List<AudioDTO>();

        _audioRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<AudioEntity>,
                    IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audios);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<AudioDTO>>(audios))
            .Returns(audioDtos);

        var handler = new GetAllAudiosHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(audioDtos, result.Value);
        Assert.Empty(result.Value);

        _audioRepositoryMock.Verify(repo => repo.GetAllAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<
                IQueryable<AudioEntity>,
                IIncludableQueryable<AudioEntity, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<AudioDTO>>(audios),
            Times.Once());
        _blobServiceMock.Verify(
            blob => blob.FindFileInStorageAsBase64(It.IsAny<string>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }
}
