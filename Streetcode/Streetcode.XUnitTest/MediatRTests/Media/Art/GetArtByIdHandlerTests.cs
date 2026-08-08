using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.GetById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.Media.Art;

public class GetArtByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public GetArtByIdHandlerTests()
    {
        _repositoryMock = new Mock<IRepositoryWrapper>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILoggerService>();
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenArtExists()
    {
        int expectedId = 1;
        var art = new DAL.Entities.Media.Images.Art { Id = expectedId };
        var artDto = new ArtDTO { Id = expectedId };

        _repositoryMock.Setup(r => r.ArtRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.Media.Images.Art, bool>>>(), null))
            .ReturnsAsync(art);

        _mapperMock.Setup(m => m.Map<ArtDTO>(art))
            .Returns(artDto);

        var handler = new GetArtByIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetArtByIdQuery(expectedId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedId, result.Value.Id);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenArtNotFound()
    {
        int searchId = 999;
        _repositoryMock.Setup(r => r.ArtRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.Media.Images.Art, bool>>>(), null))
            .ReturnsAsync((DAL.Entities.Media.Images.Art)null!);

        var handler = new GetArtByIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetArtByIdQuery(searchId);
        string expectedErrorMsg = $"Cannot find an art with corresponding id: {searchId}";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMsg, result.Errors.First().Message);

        _loggerMock.Verify(l => l.LogError(query, expectedErrorMsg), Times.Once);
    }
}