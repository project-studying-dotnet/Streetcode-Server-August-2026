using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;

namespace Streetcode.BLL.MediatR.HistoryMap.Create
{
    public record CreateHistoryMapRecordCommand(CreateHistoryMapRecordDTO Dto)
        : IRequest<Result<HistoryMapRecordDTO>>;
}