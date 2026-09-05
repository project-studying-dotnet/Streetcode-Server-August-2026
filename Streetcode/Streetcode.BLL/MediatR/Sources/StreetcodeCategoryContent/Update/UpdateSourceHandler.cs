using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Update;

public sealed class UpdateSourceHandler
    : IRequestHandler<UpdateSourceCommand, Result<StreetcodeCategoryContentDTO>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;
    private readonly ILoggerService _logger;

    public UpdateSourceHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<StreetcodeCategoryContentDTO>> Handle(
        UpdateSourceCommand request,
        CancellationToken cancellationToken)
    {
        SourceUpdateDTO source = request.SourceUpdateDto;

        var sourceEntity = await _repositoryWrapper
            .StreetcodeCategoryContentRepository
            .GetFirstOrDefaultAsync(
                predicate: item =>
                    item.StreetcodeId == source.StreetcodeId &&
                    item.SourceLinkCategoryId ==
                        source.SourceLinkCategoryId);

        if (sourceEntity is null)
        {
            string errorMessage =
                "Cannot find source block for streetcode id: " +
                $"{source.StreetcodeId} and category id: " +
                $"{source.SourceLinkCategoryId}";

            _logger.LogError(request, errorMessage);

            return Result.Fail<StreetcodeCategoryContentDTO>(
                new Error(errorMessage));
        }

        sourceEntity.Text = source.Text;

        _repositoryWrapper
            .StreetcodeCategoryContentRepository
            .Update(sourceEntity);

        bool isSaved =
            await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!isSaved)
        {
            const string errorMessage =
                "Failed to update source block.";

            _logger.LogError(request, errorMessage);

            return Result.Fail<StreetcodeCategoryContentDTO>(
                new Error(errorMessage));
        }

        var updatedSource =
            _mapper.Map<StreetcodeCategoryContentDTO>(sourceEntity);

        return Result.Ok(updatedSource);
    }
}
