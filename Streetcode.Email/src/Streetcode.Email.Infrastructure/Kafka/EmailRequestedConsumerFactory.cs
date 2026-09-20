using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Streetcode.Email.Infrastructure.Kafka;

public sealed class EmailRequestedConsumerFactory
    : IEmailRequestedConsumerFactory
{
    private readonly KafkaOptions _options;

    public EmailRequestedConsumerFactory(
        IOptions<KafkaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public IConsumer<string, string> Create()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxPollIntervalMs =
                _options.MaxPollIntervalMilliseconds,
        };

        return new ConsumerBuilder<string, string>(
            consumerConfig).Build();
    }
}
