using FluentResults;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode;

internal static class StreetcodeCreateUpdateChecks
{
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
