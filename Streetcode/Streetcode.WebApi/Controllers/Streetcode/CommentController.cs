using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Streetcode;

public class CommentController : BaseApiController
{
    [AuthorizeRoles(
        UserRole.MainAdministrator,
        UserRole.Administrator,
        UserRole.Moderator)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetCommentsToReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await Mediator.Send(
            new GetCommentsToReviewQuery(request),
            cancellationToken));
    }

    [AuthorizeRoles(
        UserRole.MainAdministrator,
        UserRole.Administrator,
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
