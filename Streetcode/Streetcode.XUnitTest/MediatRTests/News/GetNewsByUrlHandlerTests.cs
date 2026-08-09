using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.GetByUrl;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.GetByUrl;

public class GetNewsByUrlHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new Mock<IRepositoryWrapper> { DefaultValue = DefaultValue.Mock };
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenNewsExists()
    {
        string url = "test-url";
        var news = new DAL.Entities.News.News { Id = 1, URL = url };
        var newsDto = new NewsDTO { Id = 1, URL = url, Image = new ImageDTO { BlobName = "test.jpg" } };

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<NewsDTO>(news)).Returns(newsDto);

        var handler = new GetNewsByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetNewsByUrlQuery(url), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(url, result.Value.URL);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64("test.jpg"), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsNotFound()
    {
        string url = "not-found";
        var query = new GetNewsByUrlQuery(url);
        var expectedError = $"No news by entered Url - {url}";

        _repositoryMock.Setup(r => r.NewsRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.News.News, bool>>>(), It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync((DAL.Entities.News.News)null!);
        _mapperMock.Setup(m => m.Map<NewsDTO>(null)).Returns((NewsDTO)null!);

        var handler = new GetNewsByUrlHandler(_mapperMock.Object, _repositoryMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}