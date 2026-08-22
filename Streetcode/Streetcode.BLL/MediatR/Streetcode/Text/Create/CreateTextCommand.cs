using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Create;

public record CreateTextCommand(
    int StreetcodeId,
    TextCreateDTO Text) : IRequest<Result<TextDTO>>;