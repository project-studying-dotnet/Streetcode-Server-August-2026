using FluentResults;
using MediatR;
using Streetcode.BLL.MediatR.Media.Video.Validators;

namespace Streetcode.BLL.MediatR.Media.Video.Preview;

public class GetVideoAdminPreviewHandler
    : IRequestHandler<GetVideoForAdminPreviewCommand, Result<string>>
{
    public Task<Result<string>> Handle(
        GetVideoForAdminPreviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!YouTubeUrlHelper.TryGetVideoId(request.url, out var videoId))
        {
            return Task.FromResult(
                Result.Fail<string>("Invalid YouTube URL."));
        }

        return Task.FromResult(
            Result.Ok($"https://www.youtube.com/embed/{videoId}"));
    }
}