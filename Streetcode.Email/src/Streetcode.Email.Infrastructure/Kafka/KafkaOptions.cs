using System.ComponentModel.DataAnnotations;

namespace Streetcode.Email.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required]
    public string GroupId { get; init; } = string.Empty;

    [Required]
    public string Topic { get; init; } = string.Empty;

    [Required]
    public string DeadLetterTopic { get; init; } = string.Empty;

    [Range(1, 10)]
    public int MaxProcessingAttempts { get; init; } = 3;

    [Range(100, 60_000)]
    public int RetryDelayMilliseconds { get; init; } = 1_000;

    [Range(300_000, 3_600_000)]
    public int MaxPollIntervalMilliseconds { get; init; } = 900_000;
}
