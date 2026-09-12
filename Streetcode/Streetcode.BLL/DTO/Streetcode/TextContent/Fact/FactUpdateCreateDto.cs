namespace Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

public class FactUpdateCreateDto
{
    public string? ImageAlt { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FactContent { get; set; } = string.Empty;

    public int ImageId { get; set; }

    public int StreetcodeId { get; set; }
}
