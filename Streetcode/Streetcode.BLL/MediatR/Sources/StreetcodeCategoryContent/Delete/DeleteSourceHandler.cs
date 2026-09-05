using FluentResults;
using MediatR;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Delete;

public sealed class DeleteSourceHandler
    : IRequestHandler<DeleteSourceCommand, Result<Unit>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public DeleteSourceHandler(
        IRepositoryWrapper repositoryWrapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        DeleteSourceCommand request,
        CancellationToken cancellationToken)
    {
        var source = await _repositoryWrapper
            .StreetcodeCategoryContentRepository
            .GetFirstOrDefaultAsync(
                predicate: item =>
                    item.StreetcodeId == request.StreetcodeId &&
                    item.SourceLinkCategoryId ==
                        request.SourceLinkCategoryId);

        if (source is null)
        {
            string errorMessage =
                "Cannot find source block for streetcode id: " +
                $"{request.StreetcodeId} and category id: " +
                $"{request.SourceLinkCategoryId}";

            _logger.LogError(request, errorMessage);

            return Result.Fail<Unit>(new Error(errorMessage));
        }

        _repositoryWrapper
            .StreetcodeCategoryContentRepository
            .Delete(source);

        bool isSaved =
            await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            const string errorMessage =
                "Failed to delete source block.";

            _logger.LogError(request, errorMessage);

            return Result.Fail<Unit>(new Error(errorMessage));
        }

        return Result.Ok(Unit.Value);
    }
}
