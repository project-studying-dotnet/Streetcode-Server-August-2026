using Streetcode.Identity.Application.Features.Authentication.Logout;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Logout;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenRefreshTokenIsValid_ShouldSucceed()
    {
        var command = new LogoutCommand("valid-refresh-token");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsWhitespace_ShouldReturnRequiredError()
    {
        var command = new LogoutCommand(" ");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LogoutCommand.RefreshToken),
            error.PropertyName);

        Assert.Equal("RefreshToken.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenExceedsMaximumLength_ShouldReturnTooLongError()
    {
        var refreshToken = new string('a', 513);
        var command = new LogoutCommand(refreshToken);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("RefreshToken.TooLong", error.ErrorCode);

        Assert.Equal(
            nameof(LogoutCommand.RefreshToken),
            error.PropertyName);
    }
}
