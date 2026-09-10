using FluentResults;
using Streetcode.DAL.Entities.Media;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode;

internal static class StreetcodeRoleAssetResolver
{
    public static async Task<Result<Image?>> ResolveRoleImageAsync(
        IRepositoryWrapper repositoryWrapper,
        int? imageId,
        string roleName,
        string? requiredMimeType = null,
        string? requiredFormatName = null)
    {
        if (!imageId.HasValue)
        {
            return Result.Ok<Image?>(null);
        }

        var image = await repositoryWrapper.ImageRepository
            .GetFirstOrDefaultAsync(i => i.Id == imageId.Value);

        if (image is null)
        {
            return Result.Fail<Image?>(new Error($"{roleName} image not found."));
        }

        if (requiredMimeType is not null && image.MimeType != requiredMimeType)
        {
            return Result.Fail<Image?>(new Error($"{roleName} image must be a {requiredFormatName} file."));
        }

        repositoryWrapper.ImageRepository.Attach(image);

        return Result.Ok<Image?>(image);
    }

    public static async Task<Result<Audio?>> ResolveAudioAsync(
        IRepositoryWrapper repositoryWrapper,
        int? audioId,
        string requiredMimeType = "audio/mpeg",
        string requiredFormatName = "MP3")
    {
        if (!audioId.HasValue)
        {
            return Result.Ok<Audio?>(null);
        }

        var audio = await repositoryWrapper.AudioRepository
            .GetFirstOrDefaultAsync(a => a.Id == audioId.Value);

        if (audio is null)
        {
            return Result.Fail<Audio?>(new Error("Audio not found."));
        }

        if (audio.MimeType != requiredMimeType)
        {
            return Result.Fail<Audio?>(new Error($"Audio must be an {requiredFormatName} file."));
        }

        return Result.Ok<Audio?>(audio);
    }
}
