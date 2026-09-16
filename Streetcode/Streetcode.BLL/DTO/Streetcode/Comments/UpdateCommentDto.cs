namespace Streetcode.BLL.DTO.Streetcode.Comments;

public class UpdateCommentDto
{
    public string Text { get; set; } = null!;

    public byte[] RowVersion { get; set; } = null!;
}
