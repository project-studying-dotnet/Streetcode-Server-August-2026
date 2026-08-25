using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Infrastructure.Messaging.Kafka;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Persistence.Outbox;

[Collection(MsSqlCollection.Name)]
public sealed class OutboxPublisherIntegrationTests
{
    private static readonly DateTimeOffset ProcessedAt =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly MsSqlContainerFixture _fixture;

    public OutboxPublisherIntegrationTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishPendingAsync_WhenKafkaSucceeds_ShouldPublishAndMarkMessageProcessed()
    {
        await using var context = CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();

        var message = CreateMessage();
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var producer = new RecordingKafkaMessageProducer();
        var publisher = CreatePublisher(context, producer);

        await publisher.PublishPendingAsync(CancellationToken.None);

        context.ChangeTracker.Clear();
        var persistedMessage = await context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == message.Id);

        var publishedMessage = Assert.Single(producer.Messages);
        Assert.Equal("identity.user-access-changed.v1", publishedMessage.Topic);
        Assert.Equal(message.Key, publishedMessage.Key);
        Assert.Equal(message.Payload, publishedMessage.Payload);
        Assert.Equal(ProcessedAt, persistedMessage.ProcessedAt);
        Assert.Equal(0, persistedMessage.RetryCount);
        Assert.Null(persistedMessage.LastError);
    }

    [Fact]
    public async Task PublishPendingAsync_WhenKafkaFails_ShouldMarkFailedAndKeepLaterMessageWithSameKeyPending()
    {
        await using var context = CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();

        const string key = "11111111-1111-1111-1111-111111111111";
        var firstMessage = CreateMessage(
            key,
            new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero));
        var secondMessage = CreateMessage(
            key,
            new DateTimeOffset(2026, 8, 25, 10, 1, 0, TimeSpan.Zero));

        context.OutboxMessages.AddRange(firstMessage, secondMessage);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var producer = new RecordingKafkaMessageProducer(
            new InvalidOperationException("Kafka broker unavailable"));
        var publisher = CreatePublisher(context, producer);

        await publisher.PublishPendingAsync(CancellationToken.None);

        context.ChangeTracker.Clear();
        var persistedMessages = await context.OutboxMessages
            .AsNoTracking()
            .OrderBy(message => message.OccurredAt)
            .ToListAsync();

        Assert.Single(producer.Messages);
        Assert.Equal(1, persistedMessages[0].RetryCount);
        Assert.Equal("Kafka broker unavailable", persistedMessages[0].LastError);
        Assert.Null(persistedMessages[0].ProcessedAt);
        Assert.Equal(0, persistedMessages[1].RetryCount);
        Assert.Null(persistedMessages[1].LastError);
        Assert.Null(persistedMessages[1].ProcessedAt);
    }

    private StreetcodeIdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        return new StreetcodeIdentityDbContext(options);
    }

    private static OutboxPublisher CreatePublisher(
        StreetcodeIdentityDbContext context,
        IKafkaMessageProducer producer)
    {
        var options = Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            Topics = new Dictionary<string, string>
            {
                ["UserAccessChangedV1"] = "identity.user-access-changed.v1",
            },
            OutboxBatchSize = 50,
            OutboxPollingInterval = TimeSpan.FromSeconds(5),
        });

        return new OutboxPublisher(
            context,
            producer,
            options,
            new FixedTimeProvider(ProcessedAt),
            NullLogger<OutboxPublisher>.Instance);
    }

    private static OutboxMessage CreateMessage(
        string key = "11111111-1111-1111-1111-111111111111",
        DateTimeOffset? occurredAt = null)
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            "UserAccessChangedV1",
            key,
            "{\"isActive\":false}",
            occurredAt ?? new DateTimeOffset(
                2026, 8, 25, 10, 0, 0, TimeSpan.Zero));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed record PublishedMessage(
        string Topic,
        string Key,
        string Payload);

    private sealed class RecordingKafkaMessageProducer : IKafkaMessageProducer
    {
        private readonly Exception? _exceptionToThrow;

        public RecordingKafkaMessageProducer(Exception? exceptionToThrow = null)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public List<PublishedMessage> Messages { get; } = [];

        public Task PublishAsync(
            string topic,
            string key,
            string payload,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add(new PublishedMessage(topic, key, payload));

            return _exceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(_exceptionToThrow);
        }
    }
}
