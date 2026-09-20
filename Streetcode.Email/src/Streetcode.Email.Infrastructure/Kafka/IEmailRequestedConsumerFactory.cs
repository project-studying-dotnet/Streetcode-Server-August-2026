using Confluent.Kafka;

namespace Streetcode.Email.Infrastructure.Kafka;

public interface IEmailRequestedConsumerFactory
{
    IConsumer<string, string> Create();
}