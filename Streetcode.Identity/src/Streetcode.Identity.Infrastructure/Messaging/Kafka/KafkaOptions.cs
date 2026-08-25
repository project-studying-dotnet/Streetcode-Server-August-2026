namespace Streetcode.Identity.Infrastructure.Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;
    public Dictionary<string, string> Topics { get; init; } = [];
    public int OutboxBatchSize { get; init; } = 50;
    public TimeSpan OutboxPollingInterval { get; init; } = TimeSpan.FromSeconds(5);
}
