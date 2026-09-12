using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;

public record ReorderFactsCommand(FactReorderDto Reorder)
    : IRequest<Result<Unit>>;
