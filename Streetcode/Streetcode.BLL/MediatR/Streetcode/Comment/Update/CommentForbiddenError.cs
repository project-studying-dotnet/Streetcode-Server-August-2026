using FluentResults;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Update;

public sealed class CommentForbiddenError : Error
{
    public CommentForbiddenError(int commentId)
        : base($"You do not have permission to update comment with id: {commentId}")
    {
    }
}
