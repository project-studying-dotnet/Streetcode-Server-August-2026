using System.Data;
using AutoMapper;
using FluentResults;
using MediatR;
using NLog.Fluent;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode
{
    public class GetHistoryMapRecordsByStreetcodeHandler
        : IRequestHandler<GetHistoryMapRecordsByStreetcodeQuery, Result<IEnumerable<HistoryMapRecordDTO>>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IMapper _mapper;
        private readonly ILoggerService _logger;

        public GetHistoryMapRecordsByStreetcodeHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<HistoryMapRecordDTO>>> Handle(
            GetHistoryMapRecordsByStreetcodeQuery request,
            CancellationToken cancellationToken)
        {
            var streetcode = await _repositoryWrapper.StreetcodeRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == request.StreetcodeId);

            if (streetcode is null)
            {
                string errorMessage = $"Cannot find streetcode with id: {request.StreetcodeId}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<IEnumerable<HistoryMapRecordDTO>>(new Error(errorMessage));
            }

            var records = await _repositoryWrapper.HistoryMapRecordRepository
                .GetByStreetcodeIdAsync(request.StreetcodeId);

            var dtos = _mapper.Map<IEnumerable<HistoryMapRecordDTO>>(records);
            return Result.Ok(dtos);
        }
    }
}