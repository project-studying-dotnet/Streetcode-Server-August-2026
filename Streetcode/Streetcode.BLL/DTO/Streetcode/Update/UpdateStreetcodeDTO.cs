using Streetcode.DAL.Enums;
using Streetcode.BLL.DTO.AdditionalContent.Tag;

namespace Streetcode.BLL.DTO.Streetcode.Update
{
    public class UpdateStreetcodeDTO
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Title { get; set; }
        public StreetcodeType StreetcodeType { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime EventStartOrPersonBirthDate { get; set; }
        public DateTime? EventEndOrPersonDeathDate { get; set; }
        public string DateString { get; set; }
        public string Teaser { get; set; }
        public string TransliterationUrl { get; set; }
        public IEnumerable<StreetcodeTagDTO> Tags { get; set; }
    }
}
