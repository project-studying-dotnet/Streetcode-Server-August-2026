using Streetcode.Identity.Application.Features.Authentication.Refresh;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Refresh;

public sealed class RefreshSessionCommandValidatorTests
{
    private readonly RefreshSessionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenRefreshTokenIsValid_ShouldSucceed()
    {
        var command = new RefreshSessionCommand("valid-refresh-token");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsWhitespace_ShouldReturnRequiredError()
    {
        var command = new RefreshSessionCommand(" ");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(RefreshSessionCommand.RefreshToken),
            error.PropertyName);

        Assert.Equal("RefreshToken.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenExceedsMaximumLength_ShouldReturnTooLongError()
    {
        var refreshToken = new string('a', 513);
        var command = new RefreshSessionCommand(refreshToken);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("RefreshToken.TooLong", error.ErrorCode);

        Assert.Equal(
            nameof(RefreshSessionCommand.RefreshToken),
            error.PropertyName);
    }
}
