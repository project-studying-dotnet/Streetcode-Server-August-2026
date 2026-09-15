using FluentResults;
using FluentValidation;
using Streetcode.Identity.Application.Common.Behaviors;
using Streetcode.Identity.Application.Features.Registration;

namespace Streetcode.Identity.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ShouldThrowAndNotCallNext()
    {
        var request = new RegisterUserCommand(
            string.Empty,
            "ValidPassword123!",
            null);

        IValidator<RegisterUserCommand>[] validators =
        [
            new RegisterUserCommandValidator(),
        ];

        var behavior = new ValidationBehavior<
            RegisterUserCommand,
            Result<RegisterUserResponse>>(validators);

        var nextWasCalled = false;

        Task<Result<RegisterUserResponse>> Next(CancellationToken _)
        {
            nextWasCalled = true;

            var response = new RegisterUserResponse(
                Guid.Empty,
                request.Email);

            return Task.FromResult(Result.Ok(response));
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                request,
                Next,
                CancellationToken.None));

        Assert.False(nextWasCalled);

        Assert.Contains(exception.Errors, error =>
            error.ErrorCode == "Email.Required");
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCallNextReturnResponseAndForwardCancellationToken()
    {
        const string email = "user@example.com";

        var request = new RegisterUserCommand(
            email,
            "ValidPassword123!",
            "+380501234567");

        IValidator<RegisterUserCommand>[] validators =
        [
            new RegisterUserCommandValidator(),
        ];

        var behavior = new ValidationBehavior<
            RegisterUserCommand,
            Result<RegisterUserResponse>>(validators);

        var expectedResponse = new RegisterUserResponse(
            Guid.NewGuid(),
            email);

        var expectedResult = Result.Ok(expectedResponse);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var nextWasCalled = false;
        var receivedCancellationToken = CancellationToken.None;

        Task<Result<RegisterUserResponse>> Next(
            CancellationToken token)
        {
            nextWasCalled = true;
            receivedCancellationToken = token;

            return Task.FromResult(expectedResult);
        }

        var actualResult = await behavior.Handle(
            request,
            Next,
            cancellationToken);

        Assert.True(nextWasCalled);
        Assert.True(actualResult.IsSuccess);
        Assert.Equal(expectedResponse, actualResult.Value);
        Assert.Equal(cancellationToken, receivedCancellationToken);
    }

    [Fact]
    public async Task Handle_WhenNoValidatorsAreRegistered_ShouldCallNextAndForwardCancellationToken()
    {
        const string email = "user@example.com";

        var request = new RegisterUserCommand(
            email,
            "ValidPassword123!",
            "+380501234567");

        IValidator<RegisterUserCommand>[] validators = [];

        var behavior = new ValidationBehavior<
            RegisterUserCommand,
            Result<RegisterUserResponse>>(validators);

        var expectedResponse = new RegisterUserResponse(
            Guid.NewGuid(),
            email);

        var expectedResult = Result.Ok(expectedResponse);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var nextWasCalled = false;
        var receivedCancellationToken = CancellationToken.None;

        Task<Result<RegisterUserResponse>> Next(
            CancellationToken token)
        {
            nextWasCalled = true;
            receivedCancellationToken = token;

            return Task.FromResult(expectedResult);
        }

        var actualResult = await behavior.Handle(
            request,
            Next,
            cancellationToken);

        Assert.True(nextWasCalled);
        Assert.True(actualResult.IsSuccess);
        Assert.Equal(expectedResponse, actualResult.Value);
        Assert.Equal(cancellationToken, receivedCancellationToken);
    }
}
