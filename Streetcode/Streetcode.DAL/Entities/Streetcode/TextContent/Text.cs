using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Streetcode.DAL.Entities.Streetcode.TextContent;

[Table("texts", Schema = "streetcode")]
public class Text
{
    public const int TextContentMaxLength = 25000;
    public const int TitleMaxLength = 50;
    public const int AdditionalTextMaxLength = 200;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    [MaxLength(25000)]
    public string? TextContent { get; set; }
    [MaxLength(AdditionalTextMaxLength)]
    public string? AdditionalText { get; set; }

    [Required]
    public int StreetcodeId { get; set; }

    public StreetcodeContent? Streetcode { get; set; }
}