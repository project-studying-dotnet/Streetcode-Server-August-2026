namespace Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

public class FactDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public int StreetcodeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageAlt { get; set; }
    public int ImageId { get; set; }
    public string FactContent { get; set; } = string.Empty;
}
