using AutoMapper;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Types;
using Streetcode.BLL.DTO.Streetcode.Create;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;

namespace Streetcode.BLL.Mapping.Streetcode.Types;

public class PersonStreetcodeProfile : Profile
{
    public PersonStreetcodeProfile()
    {
        CreateMap<PersonStreetcode, PersonStreetcodeDTO>()
            .IncludeBase<StreetcodeContent, StreetcodeDTO>().ReverseMap();

        CreateMap<CreateStreetcodeDTO, PersonStreetcode>()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());
    }
}
