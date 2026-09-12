namespace Streetcode.BLL.DTO.Streetcode.HistoryMap
{
    public class CreateHistoryMapRecordDTO
    {
        public int StreetcodeId { get; set; }
        public int ToponymId { get; set; }
        public int PhysicalStreetcodeNumber { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}