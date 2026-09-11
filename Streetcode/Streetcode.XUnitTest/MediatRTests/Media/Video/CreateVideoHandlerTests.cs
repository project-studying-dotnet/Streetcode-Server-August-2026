using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Video.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Repositories.Interfaces;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Media.Video;

public class CreateVideoHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IVideoRepository> _videoRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenVideoCreated()
    {
        var videoDto = new VideoDTO
        {
            Id = 1,
            Url = "https://www.youtube.com/watch?v=test",
            Description = "Test video",
            StreetcodeId = 1
        };

        var video = new DAL.Entities.Media.Video
        {
            Id = 1,
            Url = videoDto.Url,
            Description = videoDto.Description,
            StreetcodeId = videoDto.StreetcodeId
        };

        _repositoryMock
            .Setup(r => r.VideoRepository)
            .Returns(_videoRepositoryMock.Object);

        _mapperMock
            .Setup(m => m.Map<DAL.Entities.Media.Video>(videoDto))
            .Returns(video);

        _videoRepositoryMock
            .Setup(r => r.CreateAsync(video))
            .ReturnsAsync(video);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<VideoDTO>(video))
            .Returns(videoDto);

        var handler = new CreateVideoHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var command = new CreateVideoCommand(videoDto);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(videoDto.Url, result.Value.Url);
        Assert.Equal(videoDto.StreetcodeId, result.Value.StreetcodeId);

        _videoRepositoryMock.Verify(
            r => r.CreateAsync(video),
            Times.Once);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);

        _loggerMock.Verify(
            l => l.LogError(It.IsAny<CreateVideoCommand>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenVideoCreationFails()
    {
        var videoDto = new VideoDTO
        {
            Url = "https://www.youtube.com/watch?v=test",
            Description = "Test video",
            StreetcodeId = 1
        };

        var video = new DAL.Entities.Media.Video
        {
            Url = videoDto.Url,
            Description = videoDto.Description,
            StreetcodeId = videoDto.StreetcodeId
        };

        _repositoryMock
            .Setup(r => r.VideoRepository)
            .Returns(_videoRepositoryMock.Object);

        _mapperMock
            .Setup(m => m.Map<DAL.Entities.Media.Video>(videoDto))
            .Returns(video);

        _videoRepositoryMock
            .Setup(r => r.CreateAsync(video))
            .ReturnsAsync(video);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0);

        var handler = new CreateVideoHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var command = new CreateVideoCommand(videoDto);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal("Failed to create a video", result.Errors.First().Message);

        _loggerMock.Verify(
            l => l.LogError(command, "Failed to create a video"),
            Times.Once);
    }
}