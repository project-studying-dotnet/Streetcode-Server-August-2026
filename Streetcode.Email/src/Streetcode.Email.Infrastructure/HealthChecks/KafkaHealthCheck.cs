using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Streetcode.Email.Infrastructure.Kafka;

namespace Streetcode.Email.Infrastructure.HealthChecks;

public sealed class KafkaHealthCheck : IHealthCheck
{
    private static readonly TimeSpan MetadataTimeout =
        TimeSpan.FromSeconds(5);

    private readonly IAdminClient _adminClient;
    private readonly KafkaOptions _options;

    public KafkaHealthCheck(
        IAdminClient adminClient,
        IOptions<KafkaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(adminClient);
        ArgumentNullException.ThrowIfNull(options);

        _adminClient = adminClient;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var metadata = _adminClient.GetMetadata(
                _options.Topic,
                MetadataTimeout);

            var topicMetadata = metadata.Topics
                .FirstOrDefault(topic => topic.Topic == _options.Topic);

            if (metadata.Brokers.Count == 0 ||
                topicMetadata is null ||
                topicMetadata.Error.IsError ||
                topicMetadata.Partitions.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Kafka broker or email topic is unavailable."));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Kafka broker and email topic are available."));
        }
        catch (KafkaException exception)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Kafka is unavailable.",
                    exception));
        }
    }
}
