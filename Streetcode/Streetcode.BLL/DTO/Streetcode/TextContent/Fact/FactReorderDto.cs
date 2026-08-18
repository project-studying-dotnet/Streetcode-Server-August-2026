using System.ComponentModel.DataAnnotations;

namespace Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

public class FactReorderDto
{
    [Range(1, int.MaxValue)]
    public int StreetcodeId { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> OrderedFactIds { get; set; } = new();
}
