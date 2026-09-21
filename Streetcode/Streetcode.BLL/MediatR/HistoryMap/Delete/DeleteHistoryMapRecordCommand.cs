using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;

namespace Streetcode.BLL.MediatR.HistoryMap.Delete
{
    public record DeleteHistoryMapRecordCommand(int Id)
        : IRequest<Result<Unit>>;
}