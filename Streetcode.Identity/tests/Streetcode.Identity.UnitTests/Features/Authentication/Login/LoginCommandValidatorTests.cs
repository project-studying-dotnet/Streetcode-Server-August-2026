using Streetcode.Identity.Application.Features.Authentication.Login;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Login;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = new LoginCommand(
            "user@example.com",
            "AnyPassword123!");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenEmailIsWhitespace_ShouldReturnRequiredError()
    {
        var command = new LoginCommand(
            " ",
            "AnyPassword123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LoginCommand.Email), error.PropertyName);
        Assert.Equal("Email.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldReturnInvalidError()
    {
        var command = new LoginCommand(
            "invalid-email",
            "AnyPassword123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LoginCommand.Email), error.PropertyName);
        Assert.Equal("Email.Invalid", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenEmailIsTooLong_ShouldReturnTooLongError()
    {
        var email = new string('a', 245) + "@example.com";
        var command = new LoginCommand(
            email,
            "AnyPassword123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LoginCommand.Email), error.PropertyName);
        Assert.Equal("Email.TooLong", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenPasswordIsWhitespace_ShouldReturnRequiredError()
    {
        var command = new LoginCommand(
            "user@example.com",
            " ");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LoginCommand.Password), error.PropertyName);
        Assert.Equal("Password.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenPasswordIsTooLong_ShouldReturnTooLongError()
    {
        var command = new LoginCommand(
            "user@example.com",
            new string('a', 257));

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(nameof(LoginCommand.Password), error.PropertyName);
        Assert.Equal("Password.TooLong", error.ErrorCode);
    }
}
