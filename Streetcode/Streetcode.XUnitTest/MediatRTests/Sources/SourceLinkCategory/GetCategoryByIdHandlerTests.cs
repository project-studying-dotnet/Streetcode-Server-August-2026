using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.SourceLink.GetCategoryById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory;

public class GetCategoryByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IBlobService> _blobServiceMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WithValidData_WhenDataExists()
    {
        int id = 1;
        var category = new DAL.Entities.Sources.SourceLinkCategory { Id = id };
        var dto = new SourceLinkCategoryDTO { Id = id, Image = new ImageDTO { BlobName = "test.jpg" } };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetFirstOrDefaultAsync(
            It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(category)),
            It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync(category);

        _mapperMock.Setup(m => m.Map<SourceLinkCategoryDTO>(category)).Returns(dto);
        _blobServiceMock.Setup(b => b.FindFileInStorageAsBase64("test.jpg")).Returns("base64string");

        var handler = new GetCategoryByIdHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.IsType<SourceLinkCategoryDTO>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenEntityNotFound()
    {
        int id = 1;
        var query = new GetCategoryByIdQuery(id);
        var expectedError = $"Cannot find any srcCategory by the corresponding id: {id}";
        var category = new DAL.Entities.Sources.SourceLinkCategory { Id = id };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetFirstOrDefaultAsync(
            It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(category)),
            It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
            .ReturnsAsync((DAL.Entities.Sources.SourceLinkCategory)null!);

        var handler = new GetCategoryByIdHandler(_repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}