namespace Streetcode.DAL.Entities.Streetcode;

public class Comment
{
    public int Id { get; set; }

    public int StreetcodeId { get; set; }

    public Guid AuthorId { get; set; }

    public string Text { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public StreetcodeContent Streetcode { get; set; } = null!;
}
