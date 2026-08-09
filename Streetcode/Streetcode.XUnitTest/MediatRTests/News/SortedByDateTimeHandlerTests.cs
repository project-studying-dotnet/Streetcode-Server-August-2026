using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.SortedByDateTime;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.SortedByDateTime;

public class SortedByDateTimeHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_AndSortsDescending()
    {
        var news = new List<DAL.Entities.News.News> { new(), new() };
        var newsDto = new List<NewsDTO>
        {
            new() { Id = 1, CreationDate = new DateTime(2023, 1, 1) },
            new() { Id = 2, CreationDate = new DateTime(2024, 1, 1) }
        };

        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<IEnumerable<NewsDTO>>(news)).Returns(newsDto);

        var handler = new SortedByDateTimeHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new SortedByDateTimeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value[0].Id);
        Assert.Equal(1, result.Value[1].Id);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsAreNull()
    {
        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync((IEnumerable<DAL.Entities.News.News>)null!);

        var handler = new SortedByDateTimeHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var query = new SortedByDateTimeQuery();
        var expectedError = "There are no news in the database";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}