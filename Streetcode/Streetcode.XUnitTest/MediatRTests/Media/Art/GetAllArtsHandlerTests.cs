using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.GetAll;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.Media.Art;

public class GetAllArtsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public GetAllArtsHandlerTests()
    {
        _repositoryMock = new Mock<IRepositoryWrapper>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILoggerService>();
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenArtsExist()
    {
        var arts = new List<DAL.Entities.Media.Images.Art> { new DAL.Entities.Media.Images.Art { Id = 1 } };
        var artsDto = new List<ArtDTO> { new ArtDTO { Id = 1 } };

        _repositoryMock.Setup(r => r.ArtRepository.GetAllAsync(null, null))
            .ReturnsAsync(arts);

        _mapperMock.Setup(m => m.Map<IEnumerable<ArtDTO>>(arts))
            .Returns(artsDto);

        var handler = new GetAllArtsHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllArtsQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenArtsAreNull()
    {
        _repositoryMock.Setup(r => r.ArtRepository.GetAllAsync(null, null))
            .ReturnsAsync((IEnumerable<DAL.Entities.Media.Images.Art>)null!);

        var handler = new GetAllArtsHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllArtsQuery();
        var expectedErrorMsg = "Cannot find any arts";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMsg, result.Errors.First().Message);

        _loggerMock.Verify(l => l.LogError(query, expectedErrorMsg), Times.Once);
    }
}