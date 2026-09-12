using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.UnitTests.Persistence.Outbox;

public class OutboxMessageTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldInitializePendingMessage()
    {
        var id = Guid.NewGuid();
        const string type = "UserAccessChangedV1";
        const string key = "11111111-1111-1111-1111-111111111111";
        const string payload = "{\"isActive\":true}";
        var occurredAt = new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero);

        var message = new OutboxMessage(id, type, key, payload, occurredAt);

        Assert.Equal(id, message.Id);
        Assert.Equal(type, message.Type);
        Assert.Equal(key, message.Key);
        Assert.Equal(payload, message.Payload);
        Assert.Equal(occurredAt, message.OccurredAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void MarkFailed_WhenCalled_ShouldIncrementRetryCountAndStoreError()
    {
        var message = CreateMessage();

        message.MarkFailed("Kafka broker unavailable");

        Assert.Equal(1, message.RetryCount);
        Assert.Equal("Kafka broker unavailable", message.LastError);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void MarkFailed_WhenCalledTwice_ShouldIncrementRetryCountAndKeepLatestError()
    {
        var message = CreateMessage();

        message.MarkFailed("First error");
        message.MarkFailed("Second error");

        Assert.Equal(2, message.RetryCount);
        Assert.Equal("Second error", message.LastError);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_AfterFailedAttempt_ShouldSetProcessedAtAndClearLastError()
    {
        var message = CreateMessage();
        var processedAt = new DateTimeOffset(2026, 8, 24, 10, 35, 0, TimeSpan.Zero);
        message.MarkFailed("Kafka broker unavailable");

        message.MarkProcessed(processedAt);

        Assert.Equal(processedAt, message.ProcessedAt);
        Assert.Null(message.LastError);
        Assert.Equal(1, message.RetryCount);
    }

    private static OutboxMessage CreateMessage()
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            "UserAccessChangedV1",
            "11111111-1111-1111-1111-111111111111",
            "{\"isActive\":true}",
            new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero));
    }
}
