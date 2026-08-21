using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetAll;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory;

public class GetAllCategoryNamesHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WithExpectedCount_WhenDataExists()
    {
        var categories = new List<DAL.Entities.Sources.SourceLinkCategory> { new() { Id = 1, Title = "Test" } };
        var dtos = new List<CategoryWithNameDTO> { new() { Id = 1, Title = "Test" } };

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, null)).ReturnsAsync(categories);
        _mapperMock.Setup(m => m.Map<IEnumerable<CategoryWithNameDTO>>(categories)).Returns(dtos);

        var handler = new GetAllCategoryNamesHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetAllCategoryNamesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.IsAssignableFrom<IEnumerable<CategoryWithNameDTO>>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenRepositoryReturnsNull()
    {
        var expectedError = "Categories is null";
        var query = new GetAllCategoryNamesQuery();

        _repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, null))
            .ReturnsAsync((IEnumerable<DAL.Entities.Sources.SourceLinkCategory>)null!);

        var handler = new GetAllCategoryNamesHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}