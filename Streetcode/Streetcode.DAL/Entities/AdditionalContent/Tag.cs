using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.DAL.Entities.AdditionalContent;

[Table("tags", Schema = "add_content")]
public class Tag
{
    public const int TitleMaxLength = 50;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(TitleMaxLength)]
    public string Title { get; set; }

    public IEnumerable<StreetcodeTagIndex> StreetcodeTagIndices { get; set; }

    public IEnumerable<StreetcodeContent> Streetcodes { get; set; }
}