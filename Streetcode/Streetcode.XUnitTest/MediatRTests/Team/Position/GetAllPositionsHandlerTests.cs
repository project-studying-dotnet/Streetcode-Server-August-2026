using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.Position.GetAll;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Team;

public class GetAllPositionsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPositionsExist()
    {
        var positions = new List<Positions> { new Positions { Id = 1 } };
        var positionsDto = new List<PositionDTO> { new PositionDTO { Id = 1 } };

        _repositoryMock.Setup(r => r.PositionRepository.GetAllAsync(null, null)).ReturnsAsync(positions);
        _mapperMock.Setup(m => m.Map<IEnumerable<PositionDTO>>(positions)).Returns(positionsDto);

        var handler = new GetAllPositionsHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetAllPositionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenPositionsAreNull()
    {
        _repositoryMock.Setup(r => r.PositionRepository.GetAllAsync(null, null))
            .ReturnsAsync((IEnumerable<Positions>)null!);

        var handler = new GetAllPositionsHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllPositionsQuery();
        var expectedError = "Cannot find any positions";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}