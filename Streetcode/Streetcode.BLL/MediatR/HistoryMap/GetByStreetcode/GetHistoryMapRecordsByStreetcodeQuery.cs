using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;

namespace Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode
{
    public record GetHistoryMapRecordsByStreetcodeQuery(int streetcodeId)
        : IRequest<Result<IEnumerable<HistoryMapRecordDTO>>>;
}