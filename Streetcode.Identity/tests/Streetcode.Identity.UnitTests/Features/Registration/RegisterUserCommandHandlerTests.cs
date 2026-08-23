using FluentResults;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Features.Registration;

namespace Streetcode.Identity.UnitTests.Features.Registration;

public class RegisterUserCommandHandlerTests
{
    private sealed class FakeIdentityService : IIdentityService
    {
        private readonly Result<Guid> _resultToReturn;

        public string? ReceivedEmail { get; private set; }
        public string? ReceivedPassword { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public FakeIdentityService(Result<Guid> resultToReturn)
        {
            _resultToReturn = resultToReturn;
        }

        public Task<Result<Guid>> CreateUserAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            ReceivedEmail = email;
            ReceivedPassword = password;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_resultToReturn);
        }
    }

    [Fact]
    public async Task Handle_WhenIdentityServiceSucceeds_ShouldReturnUserResponse()
    {
        var expectedId = Guid.NewGuid();
        const string email = "email@gmail.com";
        const string password = "password";

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var identityService = new FakeIdentityService(Result.Ok(expectedId));

        var handler = new RegisterUserCommandHandler(identityService);
        var command = new RegisterUserCommand(email, password);

        var result = await handler.Handle(command, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);

        Assert.Equal(email, identityService.ReceivedEmail);
        Assert.Equal(password, identityService.ReceivedPassword);
        Assert.Equal(cancellationToken, identityService.ReceivedCancellationToken);
    }

    [Fact]
    public async Task Handle_WhenIdentityServiceFails_ShouldReturnErrors()
    {
        const string message = "Email already exists";
        const string errorCode = "DuplicateEmail";

        var identityError = new Error(message)
            .WithMetadata("Code", errorCode);
        var identityService = new FakeIdentityService(Result.Fail<Guid>(identityError));

        const string email = "email@gmail.com";
        const string password = "password";

        var handler = new RegisterUserCommandHandler(identityService);
        var command = new RegisterUserCommand(email, password);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var returnedError = Assert.Single(result.Errors);

        Assert.Equal(message, returnedError.Message);
        Assert.Equal(errorCode, returnedError.Metadata["Code"]);
    }
}
