using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Newss.Update;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.News;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.News.Update;

public class UpdateNewsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenUpdateIsSuccessful_WithImage()
    {
        var newsDto = new NewsDTO { Id = 1 };
        var newsEntity = new DAL.Entities.News.News { Id = 1, Image = new Image() };
        var returnedDto = new NewsDTO { Id = 1, Image = new ImageDTO { BlobName = "test" } };
        var command = new UpdateNewsCommand(newsDto);

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _mapperMock.Setup(m => m.Map<NewsDTO>(newsEntity)).Returns(returnedDto);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _repositoryMock.Setup(r => r.NewsRepository.Update(It.IsAny<DAL.Entities.News.News>()));

        var handler = new UpdateNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.NewsRepository.Update(newsEntity), Times.Once);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64("test"), Times.Once);
        _repositoryMock.Verify(r => r.ImageRepository.Delete(It.IsAny<Image>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_AndDeletesOldImage_WhenImageIsNull_EdgeCase()
    {
        var newsDto = new NewsDTO { Id = 1 };
        var newsEntity = new DAL.Entities.News.News { Id = 1, Image = null };
        var returnedDto = new NewsDTO { Id = 1, ImageId = 5 };
        var oldImage = new Image { Id = 5 };
        var command = new UpdateNewsCommand(newsDto);

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _mapperMock.Setup(m => m.Map<NewsDTO>(newsEntity)).Returns(returnedDto);
        _repositoryMock.Setup(r => r.ImageRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Image, bool>>>(), null))
            .ReturnsAsync(oldImage);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _repositoryMock.Setup(r => r.NewsRepository.Update(It.IsAny<DAL.Entities.News.News>()));
        _repositoryMock.Setup(r => r.ImageRepository.Delete(It.IsAny<Image>()));

        var handler = new UpdateNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.ImageRepository.Delete(oldImage), Times.Once);
        _blobServiceMock.Verify(b => b.FindFileInStorageAsBase64(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenMapperReturnsNull()
    {
        var command = new UpdateNewsCommand(new NewsDTO());
        var expectedError = "Cannot convert null to news";

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(It.IsAny<NewsDTO>())).Returns((DAL.Entities.News.News)null!);

        var handler = new UpdateNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenSaveChangesFails()
    {
        var newsDto = new NewsDTO { Id = 1 };
        var newsEntity = new DAL.Entities.News.News { Id = 1 };
        var command = new UpdateNewsCommand(newsDto);
        var expectedError = "Failed to update news";

        _mapperMock.Setup(m => m.Map<DAL.Entities.News.News>(newsDto)).Returns(newsEntity);
        _mapperMock.Setup(m => m.Map<NewsDTO>(newsEntity)).Returns(new NewsDTO());

        _repositoryMock.Setup(r => r.ImageRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Image, bool>>>(), null))
            .ReturnsAsync((Image)null!);

        _repositoryMock.Setup(r => r.NewsRepository.Update(It.IsAny<DAL.Entities.News.News>()));
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var handler = new UpdateNewsHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(command, expectedError), Times.Once);
    }
}