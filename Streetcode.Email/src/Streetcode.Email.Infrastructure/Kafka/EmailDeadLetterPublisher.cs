using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Streetcode.Email.Infrastructure.Kafka;

public sealed class EmailDeadLetterPublisher
    : IEmailDeadLetterPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<EmailDeadLetterPublisher> _logger;

    public EmailDeadLetterPublisher(
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        ILogger<EmailDeadLetterPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        ConsumeResult<string, string> consumeResult,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(consumeResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        var headers = new Headers
        {
            {
                "source-topic",
                Encoding.UTF8.GetBytes(consumeResult.Topic)
            },
            {
                "source-partition",
                Encoding.UTF8.GetBytes(
                    consumeResult.Partition.Value.ToString(
                        CultureInfo.InvariantCulture))
            },
            {
                "source-offset",
                Encoding.UTF8.GetBytes(
                    consumeResult.Offset.Value.ToString(
                        CultureInfo.InvariantCulture))
            },
            {
                "dead-letter-reason",
                Encoding.UTF8.GetBytes(reasonCode)
            },
        };

        var deadLetterMessage = new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = headers,
        };

        var deliveryResult = await _producer.ProduceAsync(
            _options.DeadLetterTopic,
            deadLetterMessage,
            cancellationToken);

        if (deliveryResult.Status != PersistenceStatus.Persisted)
        {
            throw new InvalidOperationException(
                $"Kafka did not persist the DLQ message. " +
                $"Status: {deliveryResult.Status}.");
        }

        _logger.LogWarning(
            "Kafka message from topic {SourceTopic}, " +
            "partition {Partition}, offset {Offset} " +
            "was published to DLQ with reason {ReasonCode}.",
            consumeResult.Topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            reasonCode);
    }
}
