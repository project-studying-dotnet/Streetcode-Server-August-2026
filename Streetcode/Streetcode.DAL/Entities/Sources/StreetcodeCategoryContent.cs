using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.DAL.Entities.Sources;

[Table("streetcode_categoryContent", Schema = "sources")]
public class StreetcodeCategoryContent
{
    public const int TextMaxLength = 4000;

    [Required]
    [MaxLength(TextMaxLength)]
    public string? Text { get; set; }

    [Required]
    public int SourceLinkCategoryId { get; set; }

    [Required]
    public int StreetcodeId { get; set; }

    public SourceLinkCategory? SourceLinkCategory { get; set; }
    public StreetcodeContent? Streetcode { get; set; }
}
