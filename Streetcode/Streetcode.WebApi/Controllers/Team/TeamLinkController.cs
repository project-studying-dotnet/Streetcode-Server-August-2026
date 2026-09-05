using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.Create;
using Streetcode.BLL.MediatR.Team.GetAll;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.Create;
using Streetcode.BLL.MediatR.Team.TeamMembersLinks.GetAll;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Team
{
    public class TeamLinkController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return HandleResult(await Mediator.Send(new GetAllTeamLinkQuery()));
        }

        [AuthorizeRoles(UserRole.MainAdministrator, UserRole.Administrator)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TeamMemberLinkDTO teamMemberLink)
        {
            return HandleResult(await Mediator.Send(new CreateTeamLinkQuery(teamMemberLink)));
        }
    }
}
