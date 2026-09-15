using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Reply;

public record CreateReplyCommand(
    int ParentCommentId,
    Guid AuthorId,
    CreateCommentDto Reply)
    : IRequest<Result<CommentDto>>;
