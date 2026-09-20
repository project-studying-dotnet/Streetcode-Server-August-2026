using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Media.Art;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;

public record AttachStreetcodeArtCommand(StreetcodeArtAttachDto Attach)
    : IRequest<Result<StreetcodeArtDTO>>;
