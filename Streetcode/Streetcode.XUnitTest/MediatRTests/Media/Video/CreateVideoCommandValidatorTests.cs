using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.MediatR.Media.Video.Create;
using Streetcode.BLL.MediatR.Media.Video.Validators;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Media.Video;

public class CreateVideoCommandValidatorTests
{
    private readonly CreateVideoCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ReturnsError_WhenVideoIsNull()
    {
        var command = new CreateVideoCommand(null!);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Video is required.");
    }

    [Fact]
    public async Task Validate_ReturnsError_WhenUrlIsEmpty()
    {
        var command = new CreateVideoCommand(new VideoDTO
        {
            Url = string.Empty
        });

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Video URL is required.");
    }

    [Fact]
    public async Task Validate_ReturnsError_WhenUrlIsNotYoutube()
    {
        var command = new CreateVideoCommand(new VideoDTO
        {
            Url = "https://example.com/video"
        });

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Only YouTube URLs are allowed.");
    }

    [Theory]
    [InlineData("https://youtube.com/watch?v=test")]
    [InlineData("https://www.youtube.com/watch?v=test")]
    [InlineData("https://youtu.be/test")]
    [InlineData("https://m.youtube.com/watch?v=test")]
    public async Task Validate_ReturnsValid_WhenUrlIsYoutube(string url)
    {
        var command = new CreateVideoCommand(new VideoDTO
        {
            Url = url
        });

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ReturnsError_WhenYoutubeUrlUsesHttp()
    {
        var command = new CreateVideoCommand(new VideoDTO
        {
            Url = "http://youtube.com/watch?v=test"
        });

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Only YouTube URLs are allowed.");
    }
}
