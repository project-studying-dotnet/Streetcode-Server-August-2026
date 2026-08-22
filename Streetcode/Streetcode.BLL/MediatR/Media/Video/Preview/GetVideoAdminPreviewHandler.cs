using FluentResults;
using MediatR;

namespace Streetcode.BLL.MediatR.Media.Video.Preview;

public class GetVideoAdminPreviewHandler
    : IRequestHandler<GetVideoForAdminPreviewCommand, Result<string>>
{
    public Task<Result<string>> Handle(
        GetVideoForAdminPreviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.url, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(
                Result.Fail<string>("Invalid video URL."));
        }

        var videoId = uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath.Trim('/')
            : Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)["v"].ToString();

        if (string.IsNullOrWhiteSpace(videoId))
        {
            return Task.FromResult(
                Result.Fail<string>("Invalid YouTube URL."));
        }

        return Task.FromResult(
            Result.Ok($"https://www.youtube.com/embed/{videoId}"));
    }
}