using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.MediatR.Media.Video.GetAll;
using Streetcode.BLL.MediatR.Media.Video.GetById;
using Streetcode.BLL.MediatR.Media.Video.GetByStreetcodeId;
using Streetcode.BLL.MediatR.Media.Video.Create;
using Streetcode.BLL.MediatR.Media.Video.Preview;

namespace Streetcode.WebApi.Controllers.Media;

public class VideoController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return HandleResult(await Mediator.Send(new GetAllVideosQuery()));
    }

    [HttpGet("{streetcodeId:int}")]
    public async Task<IActionResult> GetByStreetcodeId([FromRoute] int streetcodeId)
    {
        return HandleResult(await Mediator.Send(new GetVideoByStreetcodeIdQuery(streetcodeId)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        return HandleResult(await Mediator.Send(new GetVideoByIdQuery(id)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(VideoDTO video)
    {
        if (string.IsNullOrWhiteSpace(video.Url) ||
            !Uri.TryCreate(video.Url, UriKind.Absolute, out var uri) ||
            (uri.Host != "youtube.com" && uri.Host != "www.youtube.com"))
        {
            return BadRequest("Only YouTube links are allowed.");
        }

        return HandleResult(
            await Mediator.Send(new CreateVideoCommand(video)));
    }

    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview([FromQuery] string url)
    {
        return HandleResult(
            await Mediator.Send(new GetVideoForAdminPreviewCommand(url)));
    }
}
