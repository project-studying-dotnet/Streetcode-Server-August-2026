using Moq;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Application.EmailSending;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.UnitTests.Application.EmailSending;

public sealed class MarkEmailDeliveryAsFailedHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDeliveryIsPending_MarksDeliveryAsFailedAndSavesChanges()
    {
        var messageId = Guid.NewGuid();

        var delivery = new EmailDelivery(
            messageId,
            Guid.NewGuid(),
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>());

        var repository = new Mock<IEmailDeliveryRepository>();

        repository
            .Setup(value => value.GetByMessageIdAsync(
                messageId,
                CancellationToken.None))
            .ReturnsAsync(delivery);

        repository
            .Setup(value => value.SaveChangesAsync(
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        var handler = new MarkEmailDeliveryAsFailedHandler(
            repository.Object);

        await handler.HandleAsync(
            messageId,
            CancellationToken.None);

        Assert.Equal(
            EmailDeliveryStatus.Failed,
            delivery.Status);

        repository.Verify(
            value => value.SaveChangesAsync(
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMessageId_ThrowsArgumentExceptionWithoutUsingRepository()
    {
        var repository = new Mock<IEmailDeliveryRepository>();

        var handler = new MarkEmailDeliveryAsFailedHandler(
            repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                Guid.Empty,
                CancellationToken.None));

        Assert.Equal("messageId", exception.ParamName);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenDeliveryDoesNotExist_ThrowsInvalidOperationException()
    {
        var messageId = Guid.NewGuid();

        var repository = new Mock<IEmailDeliveryRepository>();

        repository
            .Setup(value => value.GetByMessageIdAsync(
                messageId,
                CancellationToken.None))
            .ReturnsAsync((EmailDelivery?)null);

        var handler = new MarkEmailDeliveryAsFailedHandler(
            repository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                messageId,
                CancellationToken.None));

        repository.Verify(
            value => value.GetByMessageIdAsync(
                messageId,
                CancellationToken.None),
            Times.Once);

        repository.Verify(
            value => value.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDeliveryIsAlreadySent_DoesNotSaveChanges()
    {
        var messageId = Guid.NewGuid();

        var delivery = new EmailDelivery(
            messageId,
            Guid.NewGuid(),
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>());

        delivery.MarkAsSending();
        delivery.MarkAsSent();

        var repository = new Mock<IEmailDeliveryRepository>();

        repository
            .Setup(value => value.GetByMessageIdAsync(
                messageId,
                CancellationToken.None))
            .ReturnsAsync(delivery);

        var handler = new MarkEmailDeliveryAsFailedHandler(
            repository.Object);

        await handler.HandleAsync(
            messageId,
            CancellationToken.None);

        Assert.Equal(
            EmailDeliveryStatus.Sent,
            delivery.Status);

        repository.Verify(
            value => value.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDeliveryIsAlreadyFailed_DoesNotSaveChanges()
    {
        var messageId = Guid.NewGuid();

        var delivery = new EmailDelivery(
            messageId,
            Guid.NewGuid(),
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>());

        delivery.MarkAsFailed();

        var repository = new Mock<IEmailDeliveryRepository>();

        repository
            .Setup(value => value.GetByMessageIdAsync(
                messageId,
                CancellationToken.None))
            .ReturnsAsync(delivery);

        var handler = new MarkEmailDeliveryAsFailedHandler(
            repository.Object);

        await handler.HandleAsync(
            messageId,
            CancellationToken.None);

        Assert.Equal(
            EmailDeliveryStatus.Failed,
            delivery.Status);

        repository.Verify(
            value => value.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new MarkEmailDeliveryAsFailedHandler(null!));

        Assert.Equal("repository", exception.ParamName);
    }
}
