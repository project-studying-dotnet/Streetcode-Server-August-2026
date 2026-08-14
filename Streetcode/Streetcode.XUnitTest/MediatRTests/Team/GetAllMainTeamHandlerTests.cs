using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.GetAll;
using Streetcode.DAL.Entities.Streetcode.TextContent;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Specifications.Team;
using System.Linq.Expressions;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Team;

public class GetAllMainTeamHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenMainTeamExists()
    {
        var team = new List<TeamMember> { new TeamMember { Id = 1, IsMain = true } };
        var teamDto = new List<TeamMemberDTO> { new TeamMemberDTO { Id = 1, IsMain = true } };

        _repositoryMock.Setup(r => r.TeamRepository.ListAsync(
            It.IsAny<GetAllMainTeamSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mapperMock.Setup(m => m.Map<IEnumerable<TeamMemberDTO>>(team)).Returns(teamDto);

        var handler = new GetAllMainTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllMainTeamQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenTeamIsNull()
    {
        _repositoryMock.Setup(r => r.TeamRepository.ListAsync(
            It.IsAny<GetAllMainTeamSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<TeamMember>)null!);

        var handler = new GetAllMainTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllMainTeamQuery();
        var expectedError = "Cannot find any team";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}