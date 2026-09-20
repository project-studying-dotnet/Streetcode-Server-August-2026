namespace Streetcode.WebApi.Extensions;

public static class HttpContextExtensions
{
    public static void AppendTokensToCookies(
        this HttpContext context,
        string accessToken,
        DateTimeOffset accessTokenExpiresAt,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        context.Response.Cookies.Append("accessToken", accessToken, CreateCookieOptions(accessTokenExpiresAt));
        context.Response.Cookies.Append("refreshToken", refreshToken, CreateCookieOptions(refreshTokenExpiresAt));
    }

    private static CookieOptions CreateCookieOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expiresAt,
        Path = "/",
    };
}
