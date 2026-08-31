using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Streetcode.DAL.Entities.Streetcode.TextContent;

[Table("texts", Schema = "streetcode")]
public class Text
{
    public const int TextContentMaxLength = 15000;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    [MaxLength(300)]
    public string? Title { get; set; }
    [Required]
    [MaxLength(TextContentMaxLength)]
    public string? TextContent { get; set; }
    [MaxLength(500)]
    public string? AdditionalText { get; set; }
    [Required]
    public int StreetcodeId { get; set; }
    public StreetcodeContent? Streetcode { get; set; }
}