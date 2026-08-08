using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.GetAll;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.Team;

public class GetAllTeamLinkHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenLinksExist()
    {
        var links = new List<TeamMemberLink> { new TeamMemberLink { Id = 1 } };
        var linksDto = new List<TeamMemberLinkDTO> { new TeamMemberLinkDTO { Id = 1 } };

        _repositoryMock.Setup(r => r.TeamLinkRepository.GetAllAsync(null, null)).ReturnsAsync(links);
        _mapperMock.Setup(m => m.Map<IEnumerable<TeamMemberLinkDTO>>(links)).Returns(linksDto);

        var handler = new GetAllTeamLinkHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new GetAllTeamLinkQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenLinksAreNull()
    {
        _repositoryMock.Setup(r => r.TeamLinkRepository.GetAllAsync(null, null))
            .ReturnsAsync((IEnumerable<TeamMemberLink>)null!);

        var handler = new GetAllTeamLinkHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllTeamLinkQuery();
        var expectedError = "Cannot find any team links";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}