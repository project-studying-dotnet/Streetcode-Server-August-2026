using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.GetAll;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.GetAll;

public class GetAllNewsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenNewsExist()
    {
        var blobName = "image.jpg";
        var base64 = "base64string";
        var news = new List<DAL.Entities.News.News> { new() { Id = 1 } };
        var newsDto = new List<NewsDTO> { new() { Id = 1, Image = new ImageDTO { BlobName = blobName } } };

        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(news);
        _mapperMock.Setup(m => m.Map<IEnumerable<NewsDTO>>(news)).Returns(newsDto);
        _blobServiceMock.Setup(b => b.FindFileInStorageAsBase64(blobName)).Returns(base64);

        var handler = new GetAllNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllNewsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(base64, result.Value.First().Image?.Base64);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndDoesNotCallBlobService_WhenListIsEmpty_EdgeCase()
    {
        var emptyNews = new List<DAL.Entities.News.News>();
        var emptyNewsDto = new List<NewsDTO>();

        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync(emptyNews);
        _mapperMock.Setup(m => m.Map<IEnumerable<NewsDTO>>(emptyNews)).Returns(emptyNewsDto);

        var handler = new GetAllNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllNewsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenNewsAreNull()
    {
        _repositoryMock.Setup(r => r.NewsRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.News.News>, IIncludableQueryable<DAL.Entities.News.News, object>>>()))
            .ReturnsAsync((IEnumerable<DAL.Entities.News.News>)null!);

        var query = new GetAllNewsQuery();
        var expectedError = "There are no news in the database";

        var handler = new GetAllNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}