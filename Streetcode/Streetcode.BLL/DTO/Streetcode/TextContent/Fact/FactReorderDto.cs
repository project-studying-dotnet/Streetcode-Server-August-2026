namespace Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

public class FactReorderDto
{
    public int StreetcodeId { get; set; }

    public List<int> OrderedFactIds { get; set; } = new();
}
