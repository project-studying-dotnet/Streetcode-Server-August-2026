using AutoMapper;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.DAL.Entities.Streetcode.TextContent;

namespace Streetcode.BLL.Mapping.Streetcode.TextContent;

public class FactProfile : Profile
{
    public FactProfile()
    {
        CreateMap<Fact, FactDto>()
            .ForMember(
                dest => dest.ImageAlt,
                opt => opt.MapFrom(source =>
                    source.Image != null && source.Image.ImageDetails != null
                        ? source.Image.ImageDetails.Alt
                        : null));

        CreateMap<FactUpdateCreateDto, Fact>();
    }
}
