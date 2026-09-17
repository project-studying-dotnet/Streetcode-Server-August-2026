using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.BLL.MediatR.ResultVariations;
using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
using Streetcode.BLL.MediatR.Streetcode.Comment.Update;

namespace Streetcode.WebApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator => _mediator ??=
        HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            if (result is NullResult<T>)
            {
                return Ok(result.Value);
            }

            return result.Value is null
                ? NotFound("Found result matching null")
                : Ok(result.Value);
        }

        if (result.Errors.Any(error => error is CommentNotFoundError))
        {
            return NotFound(result.Reasons);
        }

        if (result.Errors.Any(error => error is CommentForbiddenError))
        {
            return StatusCode(StatusCodes.Status403Forbidden, result.Reasons);
        }

        if (result.Errors.Any(error => error is CommentConflictError))
        {
            return Conflict(result.Reasons);
        }

        return BadRequest(result.Reasons);
    }
}
