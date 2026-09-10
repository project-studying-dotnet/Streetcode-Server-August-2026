namespace Streetcode.BLL.DTO.Streetcode.Comments;

public class CommentDto
{
    public int Id { get; set; }

    public int StreetcodeId { get; set; }

    public int? ParentCommentId { get; set; }

    public Guid AuthorId { get; set; }

    public string Text { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
