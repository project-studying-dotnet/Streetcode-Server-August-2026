using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.HistoryMap.Merge
{
    public class MergeToponymsHandler
        : IRequestHandler<MergeToponymsCommand, Result<Unit>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerService _logger;

        public MergeToponymsHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(
            MergeToponymsCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            var sourceToponym = await _repositoryWrapper.ToponymRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == dto.SourceToponymId);

            if (sourceToponym is null)
            {
                string errorMessage = $"Cannot find source toponym with id: {dto.SourceToponymId}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<Unit>(new Error(errorMessage));
            }

            var targetToponym = await _repositoryWrapper.ToponymRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == dto.TargetToponymId);

            if (targetToponym is null)
            {
                string errorMessage = $"Cannot find target toponym with id: {dto.TargetToponymId}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<Unit>(new Error(errorMessage));
            }

            var recordsToUpdate = await _repositoryWrapper.HistoryMapRecordRepository
                .GetByToponymIdAsync(dto.SourceToponymId);

            foreach(var record in recordsToUpdate)
            {
                record.ToponymId = dto.TargetToponymId;
                record.UpdatedAt = DateTime.UtcNow;
                _repositoryWrapper.HistoryMapRecordRepository.Update(record);
            }

            var sourceStreetcodeLinks = await _repositoryWrapper.StreetcodeToponymRepository
                    .GetAllAsync(predicate: x => x.ToponymId == dto.SourceToponymId);

            var targetStreetcodeIds = (await _repositoryWrapper.StreetcodeToponymRepository
                        .GetAllAsync(predicate: x => x.ToponymId == dto.TargetToponymId))
                    .Select(x => x.StreetcodeId)
                    .ToHashSet();

            foreach (var sourceLink in sourceStreetcodeLinks)
            {
                _repositoryWrapper.StreetcodeToponymRepository.Delete(sourceLink);

                if (!targetStreetcodeIds.Contains(sourceLink.StreetcodeId))
                {
                    await _repositoryWrapper.StreetcodeToponymRepository.CreateAsync(
                        new StreetcodeToponym
                        {
                            StreetcodeId = sourceLink.StreetcodeId,
                            ToponymId = dto.TargetToponymId,
                        });
                }
            }

            _repositoryWrapper.ToponymRepository.Delete(sourceToponym);

            bool isSaved = await _repositoryWrapper.SaveChangesAsync(cancellationToken) > 0;

            if (!isSaved)
            {
                string errorMessage = $"Failed to merge toponyms.";
                _logger.LogError(request, errorMessage);
                return Result.Fail<Unit>(new Error(errorMessage));
            }

            return Result.Ok(Unit.Value);
        }
    }
}