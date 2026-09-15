using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.HistoryMap.Create
{
    public class CreateHistoryMapRecordHandler
        : IRequestHandler<CreateHistoryMapRecordCommand, Result<HistoryMapRecordDTO>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IMapper _mapper;
        private readonly ILoggerService _logger;

        public CreateHistoryMapRecordHandler(
            IRepositoryWrapper repositoryWrapper,
            IMapper mapper,
            ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<HistoryMapRecordDTO>> Handle(
            CreateHistoryMapRecordCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            var streetcode = await _repositoryWrapper.StreetcodeRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == dto.StreetcodeId);

            if (streetcode is null)
            {
                string errorMessage = $"Cannot find streetcode with id: {dto.StreetcodeId}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<HistoryMapRecordDTO>(new Error(errorMessage));
            }

            var toponym = await _repositoryWrapper.ToponymRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == dto.ToponymId);

            if (toponym is null)
            {
                string errorMessage = $"Cannot find toponym with id: {dto.ToponymId}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<HistoryMapRecordDTO>(new Error(errorMessage));
            }

            var existingRecord = await _repositoryWrapper.HistoryMapRecordRepository
                .GetByStreetcodeAndNumberAsync(dto.StreetcodeId, dto.PhysicalStreetcodeNumber);

            if (existingRecord is not null)
            {
                string errorMessage = $"A history map record with physical streetcode number {dto.PhysicalStreetcodeNumber} already exists for this streetcode.";
                _logger.LogError(request, errorMessage);
                return Result.Fail<HistoryMapRecordDTO>(new Error(errorMessage));
            }

            var historyMapRecord = _mapper.Map<HistoryMapRecord>(dto);
            historyMapRecord.CretedAt = DateTime.UtcNow;
            historyMapRecord.UpdatedAt = DateTime.UtcNow;

            await _repositoryWrapper.HistoryMapRecordRepository.CreateAsync(historyMapRecord);
            bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

            if (!isSaved)
            {
                string errorMessage = $"Failed to create hisotry map record.";
                _logger.LogError(request, errorMessage);
                return Result.Fail<HistoryMapRecordDTO>(new Error(errorMessage));
            }

            var resultDto = _mapper.Map<HistoryMapRecordDTO>(historyMapRecord);
            return Result.Ok(resultDto);
        }
    }
}