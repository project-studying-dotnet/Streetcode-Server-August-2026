using Streetcode.Identity.Application.Features.Authentication.Logout;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Logout;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenRefreshTokenIsPresent_ShouldSucceed()
    {
        var command = new LogoutCommand("refresh-token");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsWhitespace_ShouldReturnRequiredError()
    {
        var command = new LogoutCommand("   ");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LogoutCommand.RefreshToken), error.PropertyName);
        Assert.Equal("RefreshToken.Required", error.ErrorCode);
    }
}
