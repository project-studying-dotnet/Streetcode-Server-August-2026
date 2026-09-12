using Confluent.Kafka;

namespace Streetcode.Identity.Infrastructure.Messaging.Kafka;

public sealed class KafkaMessageProducer : IKafkaMessageProducer
{
    private readonly IProducer<string, string> _producer;

    public KafkaMessageProducer(
        IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(
        string topic,
        string key,
        string payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var message = new Message<string, string>
        {
            Key = key,
            Value = payload,
        };

        await _producer.ProduceAsync(
            topic,
            message,
            cancellationToken);
    }
}
