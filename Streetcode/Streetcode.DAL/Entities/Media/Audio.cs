using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.DAL.Entities.Media;

[Table("audios", Schema = "media")]
public class Audio
{
    public const int TitleMaxLength = 100;
    public const int MimeTypeMaxLength = 10;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    [MaxLength(100)]
    public string? BlobName { get; set; }

    [Required]
    [MaxLength(MimeTypeMaxLength)]
    public string? MimeType { get; set; }

    [NotMapped]
    public string? Base64 { get; set; }

    public StreetcodeContent? Streetcode { get; set; }
}