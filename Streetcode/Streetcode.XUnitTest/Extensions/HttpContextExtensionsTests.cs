using Microsoft.AspNetCore.Http;
using Streetcode.WebApi.Extensions;
using Xunit;

namespace Streetcode.XUnitTest.Extensions;

public class HttpContextExtensionsTests
{
    [Fact]
    public void AppendTokensToCookies_WritesBothTokensWithSecureOptionsAndSeparateExpirations()
    {
        var context = new DefaultHttpContext();
        var accessExpiresAt = new DateTimeOffset(2027, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var refreshExpiresAt = accessExpiresAt.AddDays(7);

        context.AppendTokensToCookies("access-value", accessExpiresAt, "refresh-value", refreshExpiresAt);

        var cookies = context.Response.Headers.SetCookie.ToArray();
        Assert.Equal(2, cookies.Length);
        Assert.Contains("accessToken=access-value", cookies[0]);
        Assert.Contains("refreshToken=refresh-value", cookies[1]);
        Assert.Contains($"expires={accessExpiresAt:R}", cookies[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"expires={refreshExpiresAt:R}", cookies[1], StringComparison.OrdinalIgnoreCase);

        foreach (var cookie in cookies)
        {
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        }
    }
}
