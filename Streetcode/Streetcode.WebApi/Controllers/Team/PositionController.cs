using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Team;
using Streetcode.BLL.MediatR.Team.Create;
using Streetcode.BLL.MediatR.Team.Position.GetAll;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Team
{
    public class PositionController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return HandleResult(await Mediator.Send(new GetAllPositionsQuery()));
        }

        [AuthorizeRoles(UserRole.MainAdministrator)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PositionDTO position)
        {
            return HandleResult(await Mediator.Send(new CreatePositionQuery(position)));
        }
    }
}
