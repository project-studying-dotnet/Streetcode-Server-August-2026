using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Streetcode.DAL.Entities.AdditionalContent.Coordinates;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Toponyms;

namespace Streetcode.DAL.Entities.HistoryMap
{
    [Table("history_map_records", Schema ="streetcode")]
    public class HistoryMapRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int StreetcodeId { get; set; }
        public StreetcodeContent? Streetcode { get; set; }

        [Required]
        public int ToponymId { get; set; }
        public Toponym? Toponym { get; set; }

        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }

        [Required]
        public int PhysicalStreetcodeNumber { get; set; }

        public DateTime CretedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}