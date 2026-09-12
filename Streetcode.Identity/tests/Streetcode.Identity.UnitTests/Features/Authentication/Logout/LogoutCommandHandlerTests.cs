using FluentResults;
using Moq;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Logout;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Logout;

public sealed class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();

    [Fact]
    public async Task Handle_WhenServiceSucceeds_ShouldRevokeTokenFamilyAndReturnSuccess()
    {
        const string refreshToken = "refresh-token";

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var expectedResult = Result.Ok();

        _refreshTokenServiceMock
            .Setup(service => service.RevokeFamilyAsync(
                refreshToken,
                cancellationToken))
            .ReturnsAsync(expectedResult);

        var handler = new LogoutCommandHandler(
            _refreshTokenServiceMock.Object);

        var command = new LogoutCommand(refreshToken);

        var result = await handler.Handle(
            command,
            cancellationToken);

        Assert.Same(expectedResult, result);

        _refreshTokenServiceMock.Verify(
            service => service.RevokeFamilyAsync(
                refreshToken,
                cancellationToken),
            Times.Once);
    }
}
