using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;

namespace Streetcode.BLL.MediatR.HistoryMap.Merge
{
    public record MergeToponymsCommand(MergeToponymsDTO Dto)
        : IRequest<Result<Unit>>;
}