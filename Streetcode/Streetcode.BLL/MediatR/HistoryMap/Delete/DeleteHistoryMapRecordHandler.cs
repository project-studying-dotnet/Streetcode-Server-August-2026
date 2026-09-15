using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.HistoryMap.Delete
{
    public class DeleteHistoryMapRecordHandler
        : IRequestHandler<DeleteHistoryMapRecordCommand, Result<Unit>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerService _logger;

        public DeleteHistoryMapRecordHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(
            DeleteHistoryMapRecordCommand request,
            CancellationToken cancellationToken)
        {
            var record = await _repositoryWrapper.HistoryMapRecordRepository
                .GetFirstOrDefaultAsync(predicate: x => x.Id == request.Id);

            if (record is null)
            {
                string errorMessage = $"Cannot find history map record with id: {request.Id}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<Unit>(new Error(errorMessage));
            }

            _repositoryWrapper.HistoryMapRecordRepository.Delete(record);
            bool isSaved = await _repositoryWrapper.SaveChangesAsync() > 0;

            if (!isSaved)
            {
                string errorMessage = $"Failed to delete hisotry map record with id: {request.Id}";
                _logger.LogError(request, errorMessage);
                return Result.Fail<Unit>(new Error(errorMessage));
            }

            return Result.Ok(Unit.Value);
        }
    }
}