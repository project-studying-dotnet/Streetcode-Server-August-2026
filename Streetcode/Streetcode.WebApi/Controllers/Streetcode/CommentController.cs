using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Streetcode;

public class CommentController : BaseApiController
{
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCommentDto updateCommentDto)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var authorId))
        {
            return Unauthorized();
        }

        return HandleResult(await Mediator.Send(new UpdateCommentCommand(id, authorId, updateCommentDto)));
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
