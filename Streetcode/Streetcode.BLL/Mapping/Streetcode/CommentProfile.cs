using AutoMapper;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.BLL.Mapping.Streetcode;

public class CommentProfile : Profile
{
    public CommentProfile()
    {
        CreateMap<Comment, CommentDto>();
        CreateMap<Comment, CommentWithRepliesDto>();
    }
}
