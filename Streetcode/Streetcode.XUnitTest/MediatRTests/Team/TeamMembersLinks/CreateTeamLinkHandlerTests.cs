using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.Create;
using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Team;

public class CreateTeamLinkHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenLinkCreatedSuccessfully()
    {
        var linkDtoIn = new TeamMemberLinkDTO { TargetUrl = "url" };
        var linkEntity = new TeamMemberLink { TargetUrl = "url" };
        var linkDtoOut = new TeamMemberLinkDTO { TargetUrl = "url", Id = 1 };

        _mapperMock.Setup(m => m.Map<TeamMemberLink>(linkDtoIn)).Returns(linkEntity);
        _repositoryMock.Setup(r => r.TeamLinkRepository.Create(linkEntity)).Returns(linkEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1); // > 0
        _mapperMock.Setup(m => m.Map<TeamMemberLinkDTO>(linkEntity)).Returns(linkDtoOut);

        var handler = new CreateTeamLinkHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new CreateTeamLinkQuery(linkDtoIn), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenMapToEntityFails()
    {
        _mapperMock.Setup(m => m.Map<TeamMemberLink>(It.IsAny<TeamMemberLinkDTO>()))
            .Returns((TeamMemberLink)null!);

        var handler = new CreateTeamLinkHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new CreateTeamLinkQuery(new TeamMemberLinkDTO()), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(TestMessages.CannotConvertNullToTeamLink, result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenCreateFails()
    {
        var linkEntity = new TeamMemberLink();
        _mapperMock.Setup(m => m.Map<TeamMemberLink>(It.IsAny<TeamMemberLinkDTO>())).Returns(linkEntity);
        _repositoryMock.Setup(r => r.TeamLinkRepository.Create(linkEntity)).Returns((TeamMemberLink)null!);

        var handler = new CreateTeamLinkHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new CreateTeamLinkQuery(new TeamMemberLinkDTO()), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(TestMessages.CannotCreateTeamLink, result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailResult_WhenSaveChangesFails()
    {
        var linkEntity = new TeamMemberLink();
        _mapperMock.Setup(m => m.Map<TeamMemberLink>(It.IsAny<TeamMemberLinkDTO>())).Returns(linkEntity);
        _repositoryMock.Setup(r => r.TeamLinkRepository.Create(linkEntity)).Returns(linkEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var handler = new CreateTeamLinkHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(new CreateTeamLinkQuery(new TeamMemberLinkDTO()), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(TestMessages.FailedToCreateTeam, result.Errors.First().Message);
    }
}