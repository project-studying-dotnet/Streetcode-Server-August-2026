using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Update;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Update
{
    public record UpdateStreetcodeCommand(int Id, UpdateStreetcodeDTO updatedStreetcode) : IRequest<Result<StreetcodeDTO>>;
}
