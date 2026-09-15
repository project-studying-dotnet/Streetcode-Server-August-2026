using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Update;

public record UpdateCommentCommand(
    int Id,
    Guid AuthorId,
    UpdateCommentDto Comment)
    : IRequest<Result<CommentDto>>;
