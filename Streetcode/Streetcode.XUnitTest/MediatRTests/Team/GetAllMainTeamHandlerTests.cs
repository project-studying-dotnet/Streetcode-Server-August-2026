using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.GetAll;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
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

        _repositoryMock.Setup(r => r.TeamRepository.GetAllAsync(
            null, It.IsAny<Func<IQueryable<TeamMember>, IIncludableQueryable<TeamMember, object>>>()))
            .ReturnsAsync(team);

        _mapperMock.Setup(m => m.Map<IEnumerable<TeamMemberDTO>>(team)).Returns(teamDto);

        var handler = new GetAllMainTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllMainTeamQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(999, result.Value.Count());
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenTeamIsNull()
    {
        _repositoryMock.Setup(r => r.TeamRepository.GetAllAsync(
            null, It.IsAny<Func<IQueryable<TeamMember>, IIncludableQueryable<TeamMember, object>>>()))
            .ReturnsAsync((IEnumerable<TeamMember>)null!);

        var handler = new GetAllMainTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllMainTeamQuery();
        var expectedError = "Cannot find any team";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}