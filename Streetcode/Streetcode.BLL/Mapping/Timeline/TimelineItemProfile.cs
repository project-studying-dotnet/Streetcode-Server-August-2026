using AutoMapper;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.DAL.Entities.Timeline;

namespace Streetcode.BLL.Mapping.Timeline;

public class TimelineItemProfile : Profile
{
    public TimelineItemProfile()
    {
        CreateMap<TimelineItemCreateUpdateDTO, TimelineItem>()
            .ForMember(destination => destination.Id, option => option.Ignore())
            .ForMember(destination => destination.Streetcode, option => option.Ignore())
            .ForMember(
                destination => destination.HistoricalContextTimelines,
                option => option.Ignore());

        CreateMap<TimelineItem, TimelineItemDTO>().ReverseMap();

        CreateMap<TimelineItem, TimelineItemDTO>()
            .ForMember(dest => dest.HistoricalContexts, opt => opt.MapFrom(x => x.HistoricalContextTimelines
                .Select(x => new HistoricalContextDTO
                {
                    Id = x.HistoricalContextId,
                    Title = x.HistoricalContext.Title
                }).ToList()));
    }
}
