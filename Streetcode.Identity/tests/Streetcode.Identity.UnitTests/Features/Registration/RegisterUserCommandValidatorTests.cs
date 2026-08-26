using Streetcode.Identity.Application.Features.Registration;

namespace Streetcode.Identity.UnitTests.Features.Registration;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldSucceed()
    {
        const string email = "user@example.com";
        const string password = "ValidPassword123!";
        const string phoneNumber = "+380501112233";

        var command = new RegisterUserCommand(email, password, phoneNumber);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldReturnEmailRequiredError()
    {
        const string password = "ValidPassword123!";
        const string phoneNumber = "+380501112233";

        var command = new RegisterUserCommand(string.Empty, password, phoneNumber);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(RegisterUserCommand.Email),
            error.PropertyName);

        Assert.Equal("Email.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldReturnEmailInvalidError()
    {
        const string email = "not-an-email";
        const string password = "ValidPassword123!";
        const string phoneNumber = "+380501112233";

        var command = new RegisterUserCommand(email, password, phoneNumber);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(RegisterUserCommand.Email),
            error.PropertyName);

        Assert.Equal("Email.Invalid", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenEmailExceedsMaximumLength_ShouldReturnEmailTooLongError()
    {
        var email = $"{new string('a', 250)}@example.com";
        const string password = "ValidPassword123!";
        const string phoneNumber = "+380501112233";

        Assert.True(email.Length > 256);

        var command = new RegisterUserCommand(email, password, phoneNumber);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(RegisterUserCommand.Email),
            error.PropertyName);

        Assert.Equal("Email.TooLong", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenPasswordIsEmpty_ShouldReturnPasswordRequiredError()
    {
        const string email = "user@example.com";
        const string phoneNumber = "+380501112233";

        var command = new RegisterUserCommand(email, string.Empty, phoneNumber);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            nameof(RegisterUserCommand.Password),
            error.PropertyName);

        Assert.Equal("Password.Required", error.ErrorCode);
    }
}
