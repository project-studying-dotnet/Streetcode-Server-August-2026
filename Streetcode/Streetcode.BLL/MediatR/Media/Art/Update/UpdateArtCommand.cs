using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Media.Art;

namespace Streetcode.BLL.MediatR.Media.Art.Update;

public record UpdateArtCommand(
    int Id,
    ArtUpdateCreateDto Art)
    : IRequest<Result<ArtDTO>>;
