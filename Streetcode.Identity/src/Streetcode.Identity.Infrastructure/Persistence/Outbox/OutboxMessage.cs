namespace Streetcode.Identity.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Key { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    public OutboxMessage(
        Guid id,
        string type,
        string key,
        string payload,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type, nameof(type));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox message ID cannot be empty", nameof(id));
        }
        Id = id;
        Type = type;
        Key = key;
        Payload = payload;
        ProcessedAt = null;
        RetryCount = 0;
        LastError = null;
        OccurredAt = occurredAt;
    }

    private OutboxMessage()
    {
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        RetryCount = checked(RetryCount + 1);
        LastError = error;
    }
}
