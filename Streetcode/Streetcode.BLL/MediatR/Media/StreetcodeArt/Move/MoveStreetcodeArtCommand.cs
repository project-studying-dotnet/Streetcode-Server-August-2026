using FluentResults;
using MediatR;
using Streetcode.BLL.Enums;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;

public record MoveStreetcodeArtCommand(int StreetcodeId, int ArtId, MoveDirection? Direction)
    : IRequest<Result<Unit>>;
