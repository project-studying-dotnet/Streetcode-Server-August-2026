using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.Enums;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.GetByStreetcodeId;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;

namespace Streetcode.WebApi.Controllers.Media.Images;

public class StreetcodeArtController : BaseApiController
{
    [HttpGet("{streetcodeId:int}")]
    public async Task<IActionResult> GetByStreetcodeId([FromRoute] int streetcodeId)
    {
        return HandleResult(await Mediator.Send(new GetStreetcodeArtByStreetcodeIdQuery(streetcodeId)));
    }

    [HttpPost]
    public async Task<IActionResult> Attach([FromBody] StreetcodeArtAttachDto attach)
    {
        return HandleResult(await Mediator.Send(new AttachStreetcodeArtCommand(attach)));
    }

    [HttpDelete("{streetcodeId:int}/{artId:int}")]
    public async Task<IActionResult> Detach(
        [FromRoute] int streetcodeId,
        [FromRoute] int artId)
    {
        return HandleResult(await Mediator.Send(new DetachStreetcodeArtCommand(streetcodeId, artId)));
    }

    [HttpPut("{streetcodeId:int}/{artId:int}")]
    public async Task<IActionResult> Move(
        [FromRoute] int streetcodeId,
        [FromRoute] int artId,
        [FromQuery] MoveDirection? direction)
    {
        return HandleResult(await Mediator.Send(new MoveStreetcodeArtCommand(streetcodeId, artId, direction)));
    }
}
