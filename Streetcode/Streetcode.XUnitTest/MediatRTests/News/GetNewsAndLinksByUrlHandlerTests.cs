using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.GetNewsAndLinksByUrl;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.GetNewsAndLinksByUrl;

public class GetNewsAndLinksByUrlHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenArrayHasMoreThanThreeItems()
    {
        var url = "url2";
        var news = new DAL.Entities.News.News { Id = 2, URL = url };
        var newsDto = new NewsDTO { Id = 2, URL = url };

        var allNews = new List<DAL.Entities.News.News>
        {
            new() { Id = 1, URL = "url1", Title = "T1" },
            new() { Id = 2, URL = "url2", Title = "T2" },
            new() { Id = 3, URL = "url3", Title = "T3" },
            new() { Id = 4, URL = "url4", Title = "T4" }
        };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);
        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, null)).ReturnsAsync(allNews);

        var handler = new GetNewsAndLinksByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsAndLinksByUrlQuery(url), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("url1", result.Value.PrevNewsUrl);
        Assert.Equal("url3", result.Value.NextNewsUrl);
        Assert.Equal("url4", result.Value.RandomNews.RandomNewsUrl);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenArrayHasOnlyOneItem_EdgeCase()
    {
        var url = "url1";
        var news = new DAL.Entities.News.News { Id = 1, URL = url };
        var newsDto = new NewsDTO { Id = 1, URL = url };
        var allNews = new List<DAL.Entities.News.News> { new() { Id = 1, URL = url, Title = "T1" } };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);
        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, null)).ReturnsAsync(allNews);

        var handler = new GetNewsAndLinksByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsAndLinksByUrlQuery(url), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PrevNewsUrl);
        Assert.Null(result.Value.NextNewsUrl);
        Assert.Equal(url, result.Value.RandomNews.RandomNewsUrl);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsNotFound()
    {
        var url = "not-found";
        var query = new GetNewsAndLinksByUrlQuery(url);
        var expectedError = $"No news by entered Url - {url}";

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync((DAL.Entities.News.News)null!);
        _mapperMock.Setup(m => m.Map<NewsDTO>(null)).Returns((NewsDTO)null!);

        var handler = new GetNewsAndLinksByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndSelectsCorrectRandomNews_WhenRequestingSecondToLastItem()
    {
        var url = "url3";
        var news = new DAL.Entities.News.News { Id = 3, URL = url };
        var newsDto = new NewsDTO { Id = 3, URL = url };

        var allNews = new List<DAL.Entities.News.News>
        {
            new() { Id = 1, URL = "url1", Title = "T1" },
            new() { Id = 2, URL = "url2", Title = "T2" },
            new() { Id = 3, URL = "url3", Title = "T3" },
            new() { Id = 4, URL = "url4", Title = "T4" }
        };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);
        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, null)).ReturnsAsync(allNews);

        var handler = new GetNewsAndLinksByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsAndLinksByUrlQuery(url), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("url2", result.Value.PrevNewsUrl);
        Assert.Equal("url4", result.Value.NextNewsUrl);
        Assert.Equal("url1", result.Value.RandomNews.RandomNewsUrl);
    }
}