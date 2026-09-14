using FluentResults;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode;

internal static class StreetcodeCreateUpdateChecks
{
    public static Result<TValue>? ExtractFailure<TValue>(
        IResultBase result,
        object request,
        ILoggerService logger)
    {
        if (!result.IsFailed)
        {
            return null;
        }

        var errorMsg = result.Errors[0].Message;
        logger.LogError(request, errorMsg);
        return Result.Fail<TValue>(errorMsg);
    }

    public static Result<TValue>? ExtractCombinedFailure<TValue>(
        IEnumerable<ResultBase> results,
        object request,
        ILoggerService logger)
    {
        var merged = Result.Merge(results.ToArray());

        if (!merged.IsFailed)
        {
            return null;
        }

        var errorMsg = string.Join(" ", merged.Errors.Select(e => e.Message));
        logger.LogError(request, errorMsg);
        return Result.Fail<TValue>(errorMsg);
    }

    public static bool HasDuplicateImageRoleIds(params int?[] imageIds)
    {
        var assignedIds = imageIds.Where(id => id.HasValue).Select(id => id!.Value).ToList();
        return assignedIds.Count != assignedIds.Distinct().Count();
    }

    public static async Task<Result> ValidateTagsExistAsync(
        IRepositoryWrapper repositoryWrapper,
        IEnumerable<int> tagIds)
    {
        var tagIdList = tagIds.ToList();

        var existingTags = await repositoryWrapper.TagRepository
            .GetAllAsync(t => tagIdList.Contains(t.Id));

        var missingTagIds = tagIdList
            .Except(existingTags.Select(t => t.Id))
            .ToList();

        if (missingTagIds.Any())
        {
            return Result.Fail(
                $"Tag(s) not found: {string.Join(", ", missingTagIds)}");
        }

        return Result.Ok();
    }

    public static async Task<Result> EnsureIndexAndUrlAreUniqueAsync(
        IRepositoryWrapper repositoryWrapper,
        int index,
        string transliterationUrl,
        int? excludeStreetcodeId = null)
    {
        var indexConflict = await repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(s =>
                s.Index == index && s.Id != excludeStreetcodeId);

        if (indexConflict is not null)
        {
            return Result.Fail(
                $"Streetcode with index {index} already exists.");
        }

        var transliterationUrlConflict = await repositoryWrapper.StreetcodeRepository
            .GetFirstOrDefaultAsync(s =>
                s.TransliterationUrl == transliterationUrl &&
                s.Id != excludeStreetcodeId);

        if (transliterationUrlConflict is not null)
        {
            return Result.Fail("Transliteration URL is already in use.");
        }

        return Result.Ok();
    }
}
