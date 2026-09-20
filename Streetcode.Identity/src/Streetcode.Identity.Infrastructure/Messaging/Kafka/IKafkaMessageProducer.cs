namespace Streetcode.Identity.Infrastructure.Messaging.Kafka;

public interface IKafkaMessageProducer
{
    Task PublishAsync(
        string topic,
        string key,
        string payload,
        CancellationToken cancellationToken);
}