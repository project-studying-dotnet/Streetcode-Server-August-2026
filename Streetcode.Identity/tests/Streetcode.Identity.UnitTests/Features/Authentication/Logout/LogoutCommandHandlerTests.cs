using FluentResults;
using Moq;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Logout;

namespace Streetcode.Identity.UnitTests.Features.Authentication.Logout;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRevokeTokenFamilyAndReturnServiceResult()
    {
        const string refreshToken = "refresh-token";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var expectedResult = Result.Ok();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        refreshTokenService
            .Setup(service => service.RevokeFamilyAsync(
                refreshToken,
                cancellationToken))
            .ReturnsAsync(expectedResult);

        var handler = new LogoutCommandHandler(refreshTokenService.Object);
        var command = new LogoutCommand(refreshToken);

        var result = await handler.Handle(command, cancellationToken);

        Assert.Same(expectedResult, result);
        refreshTokenService.Verify(service => service.RevokeFamilyAsync(
            refreshToken,
            cancellationToken), Times.Once);
    }
}
