namespace Streetcode.BLL.MediatR.Media.Video.Validators;

public static class YouTubeUrlHelper
{
    private static readonly string[] AllowedHosts =
    [
        "youtube.com",
        "www.youtube.com",
        "youtu.be",
        "m.youtube.com",
    ];

    public static bool TryGetVideoId(string? url, out string videoId)
    {
        videoId = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        videoId = uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath.Trim('/')
            : Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)["v"].ToString();

        return !string.IsNullOrWhiteSpace(videoId);
    }

    public static bool IsValid(string? url)
    {
        return TryGetVideoId(url, out _);
    }
}