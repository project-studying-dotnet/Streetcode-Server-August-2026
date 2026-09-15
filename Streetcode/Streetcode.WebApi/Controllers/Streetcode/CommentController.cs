using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
using Streetcode.BLL.MediatR.Streetcode.Comment.Reply;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Streetcode;

public class CommentController : BaseApiController
{
    [Authorize]
    [HttpPost("{parentCommentId:int}")]
    public async Task<IActionResult> CreateReply(
        [FromRoute] int parentCommentId,
        [FromBody] CreateCommentDto createCommentDto)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var authorId))
        {
            return Unauthorized();
        }

        return HandleResult(await Mediator.Send(
            new CreateReplyCommand(parentCommentId, authorId, createCommentDto)));
    }

    [AuthorizeRoles(
        UserRole.MainAdministrator,
        UserRole.Admin,
        UserRole.Moderator)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        return HandleResult(await Mediator.Send(
            new GetCommentByIdQuery(id),
            cancellationToken));
    }
}
