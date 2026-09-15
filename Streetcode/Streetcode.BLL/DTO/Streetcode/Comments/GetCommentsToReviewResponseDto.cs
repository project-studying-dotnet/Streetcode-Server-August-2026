namespace Streetcode.BLL.DTO.Streetcode.Comments;

public class GetCommentsToReviewResponseDto
{
    public int Pages { get; set; }

    public IEnumerable<CommentDto> Comments { get; set; } = Array.Empty<CommentDto>();
}
