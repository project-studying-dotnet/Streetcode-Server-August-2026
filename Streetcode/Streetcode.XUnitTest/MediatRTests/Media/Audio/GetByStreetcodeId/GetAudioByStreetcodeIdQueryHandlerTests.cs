using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.GetByStreetcodeId;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio.GetByStreetcodeId;

public class GetAudioByStreetcodeIdQueryHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerServiceMock;

    public GetAudioByStreetcodeIdQueryHandlerTests()
    {
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
        _streetcodeRepositoryMock = new Mock<IStreetcodeRepository>();
        _mapperMock = new Mock<IMapper>();
        _blobServiceMock = new Mock<IBlobService>();
        _loggerServiceMock = new Mock<ILoggerService>();
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
    {
        var query = new GetAudioByStreetcodeIdQuery(5);
        var expectedError = $"Cannot find an audio with the corresponding streetcode id: {query.StreetcodeId}";

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()))
            .ReturnsAsync((StreetcodeContent?)null);

        var handler = new GetAudioByStreetcodeIdQueryHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0].Message);

        _streetcodeRepositoryMock.Verify(
            repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()),
            Times.Once());

        _loggerServiceMock.Verify(
            logger => logger.LogError(query, expectedError),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<AudioDTO>(It.IsAny<object>()),
            Times.Never());
        _blobServiceMock.Verify(
            blob => blob.FindFileInStorageAsBase64(It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenStreetcodeHasNoAudio_ShouldReturnSuccessWithoutValue()
    {
        var query = new GetAudioByStreetcodeIdQuery(5);
        var streetcodeContent = new StreetcodeContent
        {
            Id = query.StreetcodeId,
            Audio = null,
        };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()))
            .ReturnsAsync(streetcodeContent);

        var handler = new GetAudioByStreetcodeIdQueryHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ValueOrDefault);

        _streetcodeRepositoryMock.Verify(
            repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<AudioDTO>(It.IsAny<object>()),
            Times.Never());
        _blobServiceMock.Verify(
            blob => blob.FindFileInStorageAsBase64(It.IsAny<string>()),
            Times.Never());
        _loggerServiceMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenStreetcodeHasAudio_ShouldReturnSuccess()
    {
        var query = new GetAudioByStreetcodeIdQuery(5);
        var blobName = "audio.mp3";
        var mimeType = "audio/mpeg";
        var expectedBase64 = "jdjgjdgndjrugrue";
        var audioId = 4;

        var audioEntity = new AudioEntity
        {
            Id = audioId,
            BlobName = blobName,
            MimeType = mimeType,
        };
        var streetcodeContent = new StreetcodeContent
        {
            Id = query.StreetcodeId,
            Audio = audioEntity,
        };

        var audioDto = new AudioDTO
        {
            Id = audioId,
            BlobName = blobName,
            MimeType = mimeType,
            Base64 = string.Empty,
        };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()))
            .ReturnsAsync(streetcodeContent);
        _mapperMock
            .Setup(mapper => mapper.Map<AudioDTO>(audioEntity))
            .Returns(audioDto);
        _blobServiceMock
            .Setup(blob => blob.FindFileInStorageAsBase64(blobName))
            .Returns(expectedBase64);

        var handler = new GetAudioByStreetcodeIdQueryHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _blobServiceMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(audioDto, result.Value);
        Assert.Equal(expectedBase64, result.Value.Base64);

        _streetcodeRepositoryMock.Verify(
            repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<
                    IQueryable<StreetcodeContent>,
                    IIncludableQueryable<StreetcodeContent, object>>?>()),
            Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<AudioDTO>(audioEntity), Times.AtLeastOnce());
        _blobServiceMock.Verify(blob => blob.FindFileInStorageAsBase64(blobName), Times.Once);
        _loggerServiceMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }
}
