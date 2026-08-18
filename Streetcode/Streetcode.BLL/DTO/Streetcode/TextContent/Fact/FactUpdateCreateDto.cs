using System.ComponentModel.DataAnnotations;

namespace Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

public class FactUpdateCreateDto
{
    [MaxLength(200)]
    public string? ImageDescription { get; set; }

    [Required]
    [MaxLength(68)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(600)]
    public string FactContent { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ImageId { get; set; }

    [Range(1, int.MaxValue)]
    public int StreetcodeId { get; set; }
}
