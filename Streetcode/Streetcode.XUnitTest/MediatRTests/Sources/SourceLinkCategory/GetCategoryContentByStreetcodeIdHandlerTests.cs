using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetCategoryContentByStreetcodeId;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory;

public class GetCategoryContentByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WithValidData_WhenBothStreetcodeAndContentExist()
    {
        int streetcodeId = 1;
        int categoryId = 1;
        var streetcode = new StreetcodeContent { Id = streetcodeId };
        var categoryContent = new DAL.Entities.Sources.StreetcodeCategoryContent { StreetcodeId = streetcodeId, SourceLinkCategoryId = categoryId };
        var dto = new StreetcodeCategoryContentDTO { StreetcodeId = streetcodeId, SourceLinkCategoryId = categoryId };

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(streetcode);

        _repositoryMock.Setup(r => r.StreetcodeCategoryContentRepository.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<DAL.Entities.Sources.StreetcodeCategoryContent, bool>>>(), null))
            .ReturnsAsync(categoryContent);

        _mapperMock.Setup(m => m.Map<StreetcodeCategoryContentDTO>(categoryContent))
            .Returns(dto);

        var handler = new GetCategoryContentByStreetcodeIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.IsType<StreetcodeCategoryContentDTO>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenStreetcodeNotFound()
    {
        int streetcodeId = 1;
        int categoryId = 1;
        var query = new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId);
        var expectedError = $"No such streetcode with id = {streetcodeId}";

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
            .ReturnsAsync((StreetcodeContent)null!);

        var handler = new GetCategoryContentByStreetcodeIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenStreetcodeExistsButContentIsNull()
    {
        int streetcodeId = 1;
        int categoryId = 1;
        var query = new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId);
        var streetcode = new StreetcodeContent { Id = streetcodeId };
        var expectedError = "The streetcode content is null";

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(streetcode);

        _repositoryMock.Setup(r => r.StreetcodeCategoryContentRepository.GetFirstOrDefaultAsync(
            It.IsAny<Expression<Func<DAL.Entities.Sources.StreetcodeCategoryContent, bool>>>(), null))
            .ReturnsAsync((DAL.Entities.Sources.StreetcodeCategoryContent)null!);

        var handler = new GetCategoryContentByStreetcodeIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}