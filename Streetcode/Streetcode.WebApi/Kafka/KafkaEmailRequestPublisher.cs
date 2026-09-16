using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.Email.Contracts.Events;
namespace Streetcode.WebApi.Kafka;

public sealed class KafkaEmailRequestPublisher : IEmailRequestPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly EmailKafkaOptions _options;
    private readonly ILogger<KafkaEmailRequestPublisher> _logger;

    public KafkaEmailRequestPublisher(
        IProducer<string, string> producer,
        IOptions<EmailKafkaOptions> options,
        ILogger<KafkaEmailRequestPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        EmailRequestedV1 emailRequested,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(emailRequested);

        if (emailRequested.MessageId == Guid.Empty)
        {
            throw new ArgumentException(
                "MessageId cannot be empty.",
                nameof(emailRequested));
        }

        var message = new Message<string, string>
        {
            Key = emailRequested.MessageId.ToString("D"),
            Value = JsonSerializer.Serialize(emailRequested),
        };
        var result = await _producer.ProduceAsync(
            _options.Topic,
            message,
            cancellationToken);

        if (result.Status != PersistenceStatus.Persisted)
        {
            throw new InvalidOperationException(
                $"Kafka did not persist the email request. Status: {result.Status}.");
        }

        _logger.LogInformation(
            "Email request {MessageId} was persisted to Kafka topic {Topic}, " +
            "partition {Partition}, offset {Offset}.",
            emailRequested.MessageId,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }
}
