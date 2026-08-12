using System.Linq.Expressions;
using FluentResults;
using MediatR;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.Delete;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.Delete;

public class DeleteNewsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenDeletionIsSuccessful()
    {
        int newsId = 1;
        var image = new Image { Id = 2 };
        var news = new DAL.Entities.News.News { Id = newsId, Image = image };
        var command = new DeleteNewsCommand(newsId);

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), null))
            .ReturnsAsync(news);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _repositoryMock.Setup(r => r.ImageRepository.Delete(It.IsAny<Image>()));
        _repositoryMock.Setup(r => r.NewsRepository.Delete(It.IsAny<DAL.Entities.News.News>()));

        var handler = new DeleteNewsHandler(_repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.ImageRepository.Delete(image), Times.Once);
        _repositoryMock.Verify(r => r.NewsRepository.Delete(news), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndDoesNotDeleteImage_WhenImageIsNull_EdgeCase()
    {
        int newsId = 1;
        var news = new DAL.Entities.News.News { Id = newsId, Image = null };
        var command = new DeleteNewsCommand(newsId);

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), null))
            .ReturnsAsync(news);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _repositoryMock.Setup(r => r.NewsRepository.Delete(It.IsAny<DAL.Entities.News.News>()));

        var handler = new DeleteNewsHandler(_repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.ImageRepository.Delete(It.IsAny<Image>()), Times.Never);
        _repositoryMock.Verify(r => r.NewsRepository.Delete(news), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsNotFound()
    {
        int newsId = 99;
        var command = new DeleteNewsCommand(newsId);
        var expectedError = $"No news found by entered Id - {newsId}";

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), null))
            .ReturnsAsync((DAL.Entities.News.News)null!);

        var handler = new DeleteNewsHandler(_repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenSaveChangesFails()
    {
        int newsId = 1;
        var news = new DAL.Entities.News.News { Id = newsId };
        var command = new DeleteNewsCommand(newsId);
        var expectedError = "Failed to delete a news";

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), null))
            .ReturnsAsync(news);
        _repositoryMock.Setup(r => r.NewsRepository.Delete(It.IsAny<DAL.Entities.News.News>()));

        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var handler = new DeleteNewsHandler(_repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }
}