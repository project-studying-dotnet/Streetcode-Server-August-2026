using Streetcode.BLL.MediatR.Media.Video.Preview;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Media.Video.Preview;

public class GetVideoAdminPreviewHandlerTests
{
    [Theory]
    [InlineData("https://youtube.com/watch?v=test123")]
    [InlineData("https://www.youtube.com/watch?v=test123")]
    [InlineData("https://youtu.be/test123")]
    [InlineData("https://m.youtube.com/watch?v=test123")]
    public async Task Handle_ReturnsEmbedUrl_WhenYoutubeUrlIsProvided(string url)
    {
        var handler = new GetVideoAdminPreviewHandler();

        var command = new GetVideoForAdminPreviewCommand(url);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "https://www.youtube.com/embed/test123",
            result.Value);
    }
}