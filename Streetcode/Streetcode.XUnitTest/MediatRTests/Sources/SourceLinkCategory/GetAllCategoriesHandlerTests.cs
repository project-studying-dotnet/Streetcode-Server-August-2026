using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetAll;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory;

public class GetAllCategoriesHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WithExpectedCount_WhenDataExists()
    {
        var categories = new List<DAL.Entities.Sources.SourceLinkCategory> { new() { Id = 1 } };
        var dtos = new List<SourceLinkCategoryDTO> { new() { Id = 1, Image = new ImageDTO { BlobName = "test.jpg" } } };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync(categories);
        _mapperMock.Setup(m => m.Map<IEnumerable<SourceLinkCategoryDTO>>(categories)).Returns(dtos);
        _blobServiceMock.Setup(b => b.FindFileInStorageAsBase64("test.jpg")).Returns("base64string");

        var handler = new GetAllCategoriesHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.IsAssignableFrom<IEnumerable<SourceLinkCategoryDTO>>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenRepositoryReturnsEmptyCollection()
    {
        var emptyList = new List<DAL.Entities.Sources.SourceLinkCategory>();
        var expectedError = "Categories is null";
        var query = new GetAllCategoriesQuery();

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync(emptyList);

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync((IEnumerable<DAL.Entities.Sources.SourceLinkCategory>)null!);

        var handler = new GetAllCategoriesHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}