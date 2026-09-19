using System.ComponentModel.DataAnnotations;

namespace Streetcode.WebApi.Kafka;

public sealed class EmailKafkaOptions
{
    public const string SectionName = "EmailKafka";

    [Required]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required]
    public string Topic { get; init; } = string.Empty;
}