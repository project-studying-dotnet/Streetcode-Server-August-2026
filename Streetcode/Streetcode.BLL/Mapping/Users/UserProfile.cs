using AutoMapper;
using Streetcode.BLL.DTO.Users;
using Streetcode.DAL.Entities.Users;

namespace Streetcode.BLL.Mapping.Users
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<RegisterUserDTO, User>()
            .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Email));

            CreateMap<User, UserLoginDTO>().ReverseMap();
            CreateMap<UserDTO, UserLoginDTO>().ReverseMap();
            CreateMap<User, UserDTO>().ReverseMap();
        }
    }
}
