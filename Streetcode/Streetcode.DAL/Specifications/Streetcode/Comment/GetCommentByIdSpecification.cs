using Ardalis.Specification;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.DAL.Specifications.Streetcode.Comment;

public sealed class GetCommentByIdSpecification : Specification<CommentEntity>
{
    public GetCommentByIdSpecification(int id)
    {
        Query.Where(comment => comment.Id == id);
    }
}
