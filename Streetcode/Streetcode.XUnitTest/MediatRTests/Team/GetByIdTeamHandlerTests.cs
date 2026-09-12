using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.GetById;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Specifications.Team;
using System.Linq.Expressions;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Team;

public class GetByIdTeamHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenMemberExists()
    {
        int searchId = 1;
        var member = new TeamMember { Id = searchId };
        var memberDto = new TeamMemberDTO { Id = searchId };

        _repositoryMock.Setup(r => r.TeamRepository.GetBySpecAsync(
            It.IsAny<GetByIdTeamSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _mapperMock.Setup(m => m.Map<TeamMemberDTO>(member)).Returns(memberDto);

        var handler = new GetByIdTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetByIdTeamQuery(searchId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(searchId, result.Value.Id);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenMemberNotFound()
    {
        int searchId = 999;
        _repositoryMock.Setup(r => r.TeamRepository.GetBySpecAsync(
            It.IsAny<GetByIdTeamSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamMember)null!);

        var handler = new GetByIdTeamHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetByIdTeamQuery(searchId);
        var expectedError = $"Cannot find any team with corresponding id: {searchId}";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}