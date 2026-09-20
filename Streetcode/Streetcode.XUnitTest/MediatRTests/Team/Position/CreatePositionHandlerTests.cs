using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.Create;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Team;

public class CreatePositionHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPositionCreatedSuccessfully()
    {
        var positionDto = new PositionDTO { Position = "Developer" };
        var positionEntity = new Positions { Position = "Developer" };

        _repositoryMock.Setup(r => r.PositionRepository.CreateAsync(It.IsAny<Positions>()))
            .ReturnsAsync(positionEntity);

        _repositoryMock.Setup(r => r.SaveChanges());

        _mapperMock.Setup(m => m.Map<PositionDTO>(positionEntity)).Returns(positionDto);

        var handler = new CreatePositionHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new CreatePositionQuery(positionDto), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Developer", result.Value.Position);
        _repositoryMock.Verify(r => r.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenSaveChangesThrowsException()
    {
        var positionDto = new PositionDTO { Position = "Developer" };
        var expectedError = TestMessages.DatabaseConnectionLost;

        _repositoryMock.Setup(r => r.PositionRepository.CreateAsync(It.IsAny<Positions>()))
            .ReturnsAsync(new Positions());

        _repositoryMock.Setup(r => r.SaveChanges()).Throws(new Exception(expectedError));

        var handler = new CreatePositionHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);
        var query = new CreatePositionQuery(positionDto);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}