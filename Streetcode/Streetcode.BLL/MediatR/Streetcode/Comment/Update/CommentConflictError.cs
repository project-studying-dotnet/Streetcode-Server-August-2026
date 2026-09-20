using FluentResults;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Update;

public sealed class CommentConflictError : Error
{
    public CommentConflictError(int commentId)
        : base($"Comment with id: {commentId} was updated by another user.")
    {
    }
}
