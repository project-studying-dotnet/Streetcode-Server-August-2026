using FluentResults;
using MediatR;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;

public record DetachStreetcodeArtCommand(int StreetcodeId, int ArtId)
    : IRequest<Result<Unit>>;
