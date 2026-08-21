using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.SourceLink.GetCategoriesByStreetcodeId;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory;

public class GetCategoriesByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WithExpectedCount_WhenDataExists()
    {
        int streetcodeId = 1;
        var category = new DAL.Entities.Sources.SourceLinkCategory
        {
            Id = 1,
            Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent> { new() { Id = streetcodeId } }
        };
        var categories = new List<DAL.Entities.Sources.SourceLinkCategory> { category };
        var dtos = new List<SourceLinkCategoryDTO> { new() { Id = 1, Image = new ImageDTO { BlobName = "test.jpg" } } };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(
            It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(category)),
            It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync(categories);

        _mapperMock.Setup(m => m.Map<IEnumerable<SourceLinkCategoryDTO>>(categories)).Returns(dtos);
        _blobServiceMock.Setup(b => b.FindFileInStorageAsBase64("test.jpg")).Returns("base64string");

        var handler = new GetCategoriesByStreetcodeIdHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetCategoriesByStreetcodeIdQuery(streetcodeId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.IsAssignableFrom<IEnumerable<SourceLinkCategoryDTO>>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenEntityNotFound()
    {
        int streetcodeId = 1;
        var query = new GetCategoriesByStreetcodeIdQuery(streetcodeId);
        var expectedError = $"Cant find any source category with the streetcode id {streetcodeId}";

        var categoryToMatch = new DAL.Entities.Sources.SourceLinkCategory
        {
            Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent> { new() { Id = streetcodeId } }
        };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(
            It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(categoryToMatch)),
            It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync((IEnumerable<DAL.Entities.Sources.SourceLinkCategory>)null!);

        var handler = new GetCategoriesByStreetcodeIdHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}