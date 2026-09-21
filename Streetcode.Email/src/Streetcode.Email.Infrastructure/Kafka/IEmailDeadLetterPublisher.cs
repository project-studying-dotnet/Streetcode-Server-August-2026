using Confluent.Kafka;

namespace Streetcode.Email.Infrastructure.Kafka;

public interface IEmailDeadLetterPublisher
{
    Task PublishAsync(
        ConsumeResult<string, string> consumeResult,
        string reasonCode,
        CancellationToken cancellationToken);
}
