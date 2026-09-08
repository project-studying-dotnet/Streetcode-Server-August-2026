using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Application.EmailSending;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.UnitTests.Application.EmailSending;

public sealed class SendEmailDeliveryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithEmptyMessageId_ThrowsBeforeRepository()
    {
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                Guid.Empty,
                CancellationToken.None));

        Assert.Equal("messageId", exception.ParamName);
        Assert.Empty(calls);
        Assert.Empty(sender.AttemptedDeliveries);
    }

    [Fact]
    public async Task HandleAsync_WhenDeliveryDoesNotExist_Throws()
    {
        var messageId = Guid.NewGuid();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                messageId,
                CancellationToken.None));

        Assert.Equal(
            $"Email delivery '{messageId}' was not found.",
            exception.Message);
        Assert.Equal(new[] { "Get" }, calls);
        Assert.Equal(
            new[] { messageId },
            repository.RequestedMessageIds);
        Assert.Empty(sender.AttemptedDeliveries);
    }

    [Fact]
    public async Task HandleAsync_WithPendingDelivery_SendsThenSavesSentStatus()
    {
        var delivery = CreateDelivery();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            delivery);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await handler.HandleAsync(
            delivery.MessageId,
            cancellationToken);

        Assert.Equal(EmailDeliveryStatus.Sent, delivery.Status);
        Assert.Same(
            delivery,
            Assert.Single(sender.AttemptedDeliveries));
        Assert.Equal(
            new[] { "Get", "Send:Pending", "Save:Sent" },
            calls);
        Assert.Equal(cancellationToken, repository.GetCancellationToken);
        Assert.Equal(cancellationToken, repository.SaveCancellationToken);
        Assert.Equal(cancellationToken, sender.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WithSentDelivery_DoesNotSendOrSave()
    {
        var delivery = CreateDelivery();
        delivery.MarkAsSent();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            delivery);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);

        await handler.HandleAsync(
            delivery.MessageId,
            CancellationToken.None);

        Assert.Equal(new[] { "Get" }, calls);
        Assert.Empty(sender.AttemptedDeliveries);
        Assert.Null(repository.SaveCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WithFailedDelivery_DoesNotSendOrSave()
    {
        var delivery = CreateDelivery();
        delivery.MarkAsFailed();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            delivery);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);

        await handler.HandleAsync(
            delivery.MessageId,
            CancellationToken.None);

        Assert.Equal(new[] { "Get" }, calls);
        Assert.Empty(sender.AttemptedDeliveries);
        Assert.Null(repository.SaveCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenSendingFails_LeavesPendingAndDoesNotSave()
    {
        var delivery = CreateDelivery();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            delivery);
        var expectedException = new InvalidOperationException(
            "SMTP is unavailable.");
        var sender = new FakeEmailDeliverySender(
            calls,
            expectedException);
        var handler = CreateHandler(repository, sender);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                delivery.MessageId,
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal(EmailDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(new[] { "Get", "Send:Pending" }, calls);
        Assert.Null(repository.SaveCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenSavingSentStatusFails_PropagatesException()
    {
        var delivery = CreateDelivery();
        var calls = new List<string>();
        var expectedException = new InvalidOperationException(
            "SQL storage is unavailable.");
        var repository = new FakeEmailDeliveryRepository(
            calls,
            delivery,
            expectedException);
        var sender = new FakeEmailDeliverySender(calls);
        var handler = CreateHandler(repository, sender);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                delivery.MessageId,
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal(EmailDeliveryStatus.Sent, delivery.Status);
        Assert.Equal(
            new[] { "Get", "Send:Pending", "Save:Sent" },
            calls);
    }

    private static SendEmailDeliveryHandler CreateHandler(
        IEmailDeliveryRepository repository,
        IEmailDeliverySender sender)
    {
        return new SendEmailDeliveryHandler(repository, sender);
    }

    private static EmailDelivery CreateDelivery()
    {
        return new EmailDelivery(
            Guid.Parse("e5452600-fbc7-448b-80e5-0859c56bd919"),
            Guid.Parse("c9781f6d-f50f-493a-b8b0-1bba7566ce1e"),
            new DateTimeOffset(
                2026,
                9,
                8,
                12,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>
            {
                ["From"] = "sender@example.com",
                ["Content"] = "Feedback message",
            });
    }

    private sealed class FakeEmailDeliveryRepository
        : IEmailDeliveryRepository
    {
        private readonly List<string> calls;
        private readonly EmailDelivery? delivery;
        private readonly Exception? saveException;

        public FakeEmailDeliveryRepository(
            List<string> calls,
            EmailDelivery? delivery = null,
            Exception? saveException = null)
        {
            this.calls = calls;
            this.delivery = delivery;
            this.saveException = saveException;
        }

        public List<Guid> RequestedMessageIds { get; } = new();

        public CancellationToken? GetCancellationToken { get; private set; }

        public CancellationToken? SaveCancellationToken { get; private set; }

        public Task<EmailDelivery?> GetByMessageIdAsync(
            Guid messageId,
            CancellationToken cancellationToken)
        {
            calls.Add("Get");
            RequestedMessageIds.Add(messageId);
            GetCancellationToken = cancellationToken;

            return Task.FromResult(delivery);
        }

        public Task AddAsync(
            EmailDelivery emailDelivery,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "SendEmailDeliveryHandler must not add deliveries.");
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            calls.Add($"Save:{delivery?.Status}");
            SaveCancellationToken = cancellationToken;

            return saveException is null
                ? Task.CompletedTask
                : Task.FromException(saveException);
        }
    }

    private sealed class FakeEmailDeliverySender : IEmailDeliverySender
    {
        private readonly List<string> calls;
        private readonly Exception? exceptionToThrow;

        public FakeEmailDeliverySender(
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            this.calls = calls;
            this.exceptionToThrow = exceptionToThrow;
        }

        public List<EmailDelivery> AttemptedDeliveries { get; } = new();

        public CancellationToken? CancellationToken { get; private set; }

        public Task SendAsync(
            EmailDelivery delivery,
            CancellationToken cancellationToken)
        {
            calls.Add($"Send:{delivery.Status}");
            AttemptedDeliveries.Add(delivery);
            CancellationToken = cancellationToken;

            return exceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(exceptionToThrow);
        }
    }
}
