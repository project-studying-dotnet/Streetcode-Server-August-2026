namespace Streetcode.BLL.DTO.Streetcode.HistoryMap
{
    public class HistoryMapRecordDTO
    {
        public int Id { get; set; }
        public int StreetcodeId { get; set; }
        public int ToponymId { get; set; }
        public string? ToponymName { get; set; }
        public int PhysicalStreetcodeNumber { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}