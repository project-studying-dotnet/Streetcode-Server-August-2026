using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.Create;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.Create;

public class CreateNewsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenCreationIsSuccessful()
    {
        var newsDto = new NewsDTO { Id = 1, Title = "Test" };
        var newsEntity = new DAL.Entities.News.News { Id = 1, ImageId = 5 };
        var command = new CreateNewsCommand(newsDto);

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.NewsRepository.Create(newsEntity)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<NewsDTO>(newsEntity)).Returns(newsDto);

        var handler = new CreateNewsHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newsDto.Id, result.Value.Id);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndSetsImageIdToNull_WhenImageIdIsZero_EdgeCase()
    {
        var newsDto = new NewsDTO { Id = 1, Title = "Test" };
        var newsEntity = new DAL.Entities.News.News { Id = 1, ImageId = 0 };
        var command = new CreateNewsCommand(newsDto);

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.NewsRepository.Create(newsEntity)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<NewsDTO>(newsEntity)).Returns(newsDto);

        var handler = new CreateNewsHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(newsEntity.ImageId);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenMapperReturnsNull()
    {
        var command = new CreateNewsCommand(new NewsDTO());
        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(It.IsAny<NewsDTO>())).Returns((DAL.Entities.News.News)null!);
        var expectedError = "Cannot convert null to news";

        var handler = new CreateNewsHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenSaveChangesFails()
    {
        var newsDto = new NewsDTO();
        var newsEntity = new DAL.Entities.News.News();
        var command = new CreateNewsCommand(newsDto);
        var expectedError = "Failed to create a news";

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.NewsRepository.Create(newsEntity)).Returns(newsEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var handler = new CreateNewsHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }
}