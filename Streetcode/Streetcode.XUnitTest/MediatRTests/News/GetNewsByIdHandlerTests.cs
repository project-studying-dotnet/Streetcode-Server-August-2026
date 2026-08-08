using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.GetById;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.GetById;

public class GetNewsByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenNewsExists()
    {
        int newsId = 1;
        var news = new DAL.Entities.News.News { Id = newsId };
        var newsDto = new NewsDTO { Id = newsId, Image = new ImageDTO { BlobName = "test.jpg" } };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);

        var handler = new GetNewsByIdHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsByIdQuery(newsId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newsId, result.Value.Id);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64("test.jpg"), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndDoesNotCallBlobService_WhenImageIsNull_EdgeCase()
    {
        int newsId = 1;
        var news = new DAL.Entities.News.News { Id = newsId };
        var newsDto = new NewsDTO { Id = newsId, Image = null };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);

        var handler = new GetNewsByIdHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsByIdQuery(newsId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsNotFound()
    {
        int newsId = 99;
        var query = new GetNewsByIdQuery(newsId);
        var expectedError = $"No news by entered Id - {newsId}";

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync((DAL.Entities.News.News)null!);
        _mapperMock.Setup(m => m.Map<NewsDTO>(null)).Returns((NewsDTO)null!);

        var handler = new GetNewsByIdHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}