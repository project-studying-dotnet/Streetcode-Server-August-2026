using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;

namespace Streetcode.DAL.Entities.Timeline;

[Table("timeline_items", Schema = "timeline")]
public class TimelineItem
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 600;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required]
    public DateViewPattern DateViewPattern { get; set; }

    [Required]
    [MaxLength(TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(DescriptionMaxLength)]
    public string? Description { get; set; }

    public int StreetcodeId { get; set; }

    public StreetcodeContent? Streetcode { get; set; }

    public List<HistoricalContextTimeline> HistoricalContextTimelines { get; set; } = new ();
}
