using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Update;

public record UpdateFactCommand(
    int Id,
    FactUpdateCreateDto Fact) : IRequest<Result<FactDto>>;
