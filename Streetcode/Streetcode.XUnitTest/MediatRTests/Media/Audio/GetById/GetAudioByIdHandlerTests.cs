using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.GetById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio.GetById;

public class GetAudioByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public GetAudioByIdHandlerTests()
    {
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
        _audioRepositoryMock = new Mock<IAudioRepository>();
        _mapperMock = new Mock<IMapper>();
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILoggerService>();
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.AudioRepository)
            .Returns(_audioRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAudioDoesNotExist_ShouldReturnFailure()
    {
        var query = new GetAudioByIdQuery(5);
        var expectedError = $"Cannot find an audio with corresponding id: {query.Id}";
        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<
                IQueryable<AudioEntity>,
                IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync((AudioEntity?)null);

        var handler = new GetAudioByIdHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _audioRepositoryMock.Verify(repo => repo.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<IQueryable<AudioEntity>, IIncludableQueryable<AudioEntity, object>>?>()),
                Times.Once());

        _loggerMock.Verify(logger => logger.LogError(query, expectedError), Times.Once());

        _mapperMock.Verify(mapper => mapper.Map<AudioDTO>(
            It.IsAny<AudioEntity>()),
            Times.Never());
        _blobServiceMock.Verify(blob => blob.FindFileInStorageAsBase64(
                It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioExists_ShouldReturnSuccess()
    {
        var query = new GetAudioByIdQuery(5);
        var blobName = "audio.mp3";
        var mimeType = "audio/mpeg";
        var expectedBase64 = "c2VsZpY2VyZG93bmM=";

        AudioEntity audioEntity = new AudioEntity();
        audioEntity.Id = query.Id;
        audioEntity.BlobName = blobName;
        audioEntity.MimeType = mimeType;

        var audioDto = new AudioDTO();
        audioDto.Id = query.Id;
        audioDto.BlobName = blobName;
        audioDto.MimeType = mimeType;
        audioDto.Base64 = string.Empty;

        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AudioEntity, bool>>>(),
                It.IsAny<Func<IQueryable<AudioEntity>, IIncludableQueryable<AudioEntity, object>>?>()))
            .ReturnsAsync(audioEntity);

        _mapperMock.Setup(mapper => mapper.Map<AudioDTO>(
            It.IsAny<AudioEntity>()))
            .Returns(audioDto);
        _blobServiceMock.Setup(blob => blob.FindFileInStorageAsBase64(
            blobName))
            .Returns(expectedBase64);

        var handler = new GetAudioByIdHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Same(audioDto, result.Value);
        Assert.Equal(expectedBase64, result.Value.Base64);

        _audioRepositoryMock.Verify(repo => repo.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<AudioEntity, bool>>>(),
            It.IsAny<Func<IQueryable<AudioEntity>, IIncludableQueryable<AudioEntity, object>>?>()), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<AudioDTO>(audioEntity), Times.Once());

        _blobServiceMock.Verify(blob => blob.FindFileInStorageAsBase64(blobName), Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(
                It.IsAny<object>(),
                It.IsAny<string>()),
            Times.Never());
    }
}
