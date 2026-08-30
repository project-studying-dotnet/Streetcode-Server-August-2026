using AutoMapper;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Media.Audio;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Audio.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using AudioEntity = Streetcode.DAL.Entities.Media.Audio;

namespace Streetcode.XUnitTest.MediatRTests.Media.Audio;

public class CreateAudioHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public CreateAudioHandlerTests()
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
    public async Task Handle_WhenAudioIsSaved_ShouldReturnSuccess()
    {
        var audioFileBaseDto = new AudioFileBaseCreateDTO
        {
            Title = "audio",
            BaseFormat = "jfgjutvdioo3eoxl",
            MimeType = "audio/mpeg",
            Extension = "mp3",
        };
        var command = new CreateAudioCommand(audioFileBaseDto);
        var hashBlobStorageName = "pjomncjaotiv50391nvk93jvs";
        var expectedBlobName = $"{hashBlobStorageName}.{audioFileBaseDto.Extension}";
        var audioEntity = new AudioEntity
        {
            Id = 3,
            BlobName = string.Empty,
            MimeType = audioFileBaseDto.MimeType,
        };
        var createdAudioDto = new AudioDTO
        {
            Id = audioEntity.Id,
            BlobName = expectedBlobName,
            MimeType = audioEntity.MimeType,
            Base64 = string.Empty,
        };

        _blobServiceMock
            .Setup(blob => blob.SaveFileInStorage(
                audioFileBaseDto.BaseFormat!,
                audioFileBaseDto.Title!,
                audioFileBaseDto.Extension!))
            .Returns(hashBlobStorageName);
        _mapperMock
            .Setup(mapper => mapper.Map<AudioEntity>(audioFileBaseDto))
            .Returns(audioEntity);
        _audioRepositoryMock
            .Setup(repository => repository.CreateAsync(audioEntity))
            .ReturnsAsync(audioEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<AudioDTO>(audioEntity))
            .Returns(createdAudioDto);

        var handler = new CreateAudioHandler(
            _blobServiceMock.Object,
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(createdAudioDto, result.Value);
        Assert.Equal(expectedBlobName, audioEntity.BlobName);
        Assert.Equal(expectedBlobName, result.Value.BlobName);

        _blobServiceMock.Verify(
            blob => blob.SaveFileInStorage(
                audioFileBaseDto.BaseFormat!,
                audioFileBaseDto.Title!,
                audioFileBaseDto.Extension!),
            Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<AudioEntity>(audioFileBaseDto), Times.Once());
        _audioRepositoryMock.Verify(repository => repository.CreateAsync(audioEntity), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<AudioDTO>(audioEntity), Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenAudioIsNotSaved_ShouldReturnFailure()
    {
        var audioFileBaseDto = new AudioFileBaseCreateDTO
        {
            Title = "audio",
            BaseFormat = "jfgjutvdioo3eoxl",
            MimeType = "audio/mpeg",
            Extension = "mp3",
        };
        var command = new CreateAudioCommand(audioFileBaseDto);
        var hashBlobStorageName = "pjomncjaotiv50391nvk93jvs";
        var expectedBlobName = $"{hashBlobStorageName}.{audioFileBaseDto.Extension}";
        const string expectedError = "Failed to create an audio";
        var audioEntity = new AudioEntity
        {
            Id = 3,
            BlobName = string.Empty,
            MimeType = audioFileBaseDto.MimeType,
        };
        _blobServiceMock
            .Setup(blob => blob.SaveFileInStorage(
                audioFileBaseDto.BaseFormat!,
                audioFileBaseDto.Title!,
                audioFileBaseDto.Extension!))
            .Returns(hashBlobStorageName);
        _mapperMock
            .Setup(mapper => mapper.Map<AudioEntity>(audioFileBaseDto))
            .Returns(audioEntity);
        _audioRepositoryMock
            .Setup(repository => repository.CreateAsync(audioEntity))
            .ReturnsAsync(audioEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);
        var handler = new CreateAudioHandler(
            _blobServiceMock.Object,
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0].Message);
        Assert.Equal(expectedBlobName, audioEntity.BlobName);

        _blobServiceMock.Verify(
            blob => blob.SaveFileInStorage(
                audioFileBaseDto.BaseFormat!,
                audioFileBaseDto.Title!,
                audioFileBaseDto.Extension!),
            Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<AudioEntity>(audioFileBaseDto), Times.Once());
        _audioRepositoryMock.Verify(repository => repository.CreateAsync(audioEntity), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _loggerMock.Verify(logger => logger.LogError(command, expectedError), Times.Once());
    }
}
