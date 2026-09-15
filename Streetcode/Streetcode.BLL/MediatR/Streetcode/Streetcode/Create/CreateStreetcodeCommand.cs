using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Create;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Create
{
    public record CreateStreetcodeCommand(CreateStreetcodeDTO newStreetcode) : IRequest<Result<StreetcodeDTO>>;
}
