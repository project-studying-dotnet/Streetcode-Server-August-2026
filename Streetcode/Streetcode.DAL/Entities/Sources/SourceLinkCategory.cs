using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.DAL.Entities.Sources;

[Table("source_link_categories", Schema = "sources")]
public class SourceLinkCategory
{
    public const int TitleMaxLength = 23;
    public const int ImageHashLength = 64;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    public int ImageId { get; set; }

    [MaxLength(ImageHashLength)]
    public string? ImageHash { get; set; }

    public Image? Image { get; set; }

    public List<StreetcodeContent> Streetcodes { get; set; } = new ();

    public List<StreetcodeCategoryContent> StreetcodeCategoryContents { get; set; } = new ();
}
