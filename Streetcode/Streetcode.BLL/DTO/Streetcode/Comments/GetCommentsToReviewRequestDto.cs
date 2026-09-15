namespace Streetcode.BLL.DTO.Streetcode.Comments;

public class GetCommentsToReviewRequestDto
{
    public int Page { get; set; } = 1;

    public int Amount { get; set; } = 10;
}
