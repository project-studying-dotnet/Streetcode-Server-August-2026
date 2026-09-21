using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;

namespace Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode
{
    public record GetHistoryMapRecordsByStreetcodeQuery(int StreetcodeId)
        : IRequest<Result<IEnumerable<HistoryMapRecordDTO>>>;
}