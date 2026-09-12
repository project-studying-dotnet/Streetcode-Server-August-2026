using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.Toponyms;

namespace Streetcode.DAL.Entities.Streetcode.HistoryMap
{
    [Table("history_map_records", Schema = "streetcode")]
    public class HistoryMapRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int StreetcodeId { get; set; }
        [Required]
        public int ToponymId { get; set; }
        [Required]
        public int PhysicalStreetcodeNumber { get; set; }
        [Required]
        public decimal Latitude { get; set; }
        [Required]
        public decimal Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public StreetcodeContent? Streetcode { get; set; }
        public Toponym? Toponym { get; set; }
    }
}