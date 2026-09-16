namespace Streetcode.BLL.DTO.Streetcode.Comments;

public class CommentWithRepliesDto : CommentDto
{
    public IEnumerable<CommentDto> Replies { get; set; } = Array.Empty<CommentDto>();
}
