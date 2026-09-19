using AutoMapper;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.DAL.Entities.HistoryMap;

namespace Streetcode.BLL.Mapping.HistoryMap
{
    public class HistoryMapRecordProfile : Profile
    {
        public HistoryMapRecordProfile()
        {
            CreateMap<HistoryMapRecord, HistoryMapRecordDTO>()
                .ForMember(dest => dest.ToponymName, opt => opt.MapFrom(src => src.Toponym!.StreetName));

            CreateMap<HistoryMapRecordDTO, HistoryMapRecord>();

            CreateMap<CreateHistoryMapRecordDTO, HistoryMapRecord>();
        }
    }
}