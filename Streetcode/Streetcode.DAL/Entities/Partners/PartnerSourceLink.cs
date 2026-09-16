using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Enums;

namespace Streetcode.DAL.Entities.Partners;

[Table("partner_source_links", Schema = "partners")]
public class PartnerSourceLink
{
    public const int TargetUrlMaxLength = 255;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public LogoType LogoType { get; set; }

    [Required]
    [MaxLength(TargetUrlMaxLength)]
    public string? TargetUrl { get; set; }

    [Required]
    public int PartnerId { get; set; }

    public Partner? Partner { get; set; }
}