using FluentResults;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Delete;

public sealed class CommentNotFoundError : Error
{
    public CommentNotFoundError(int commentId)
        : base($"Cannot find a comment with corresponding id: {commentId}")
    {
    }
}
