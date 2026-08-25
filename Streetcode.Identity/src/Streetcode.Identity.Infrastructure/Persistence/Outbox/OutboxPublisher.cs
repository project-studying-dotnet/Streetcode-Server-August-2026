using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Infrastructure.Messaging.Kafka;

namespace Streetcode.Identity.Infrastructure.Persistence.Outbox;

public sealed class OutboxPublisher
{
    private const int MaxStoredErrorLength = 2000;

    private readonly StreetcodeIdentityDbContext _dbContext;
    private readonly IKafkaMessageProducer _producer;
    private readonly KafkaOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        StreetcodeIdentityDbContext dbContext,
        IKafkaMessageProducer producer,
        IOptions<KafkaOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxPublisher> logger)
    {
        _dbContext = dbContext;
        _producer = producer;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        var messages = await _dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        var failedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            if (failedKeys.Contains(message.Key))
            {
                continue;
            }

            try
            {
                var topic = ResolveTopic(message.Type);

                await _producer.PublishAsync(
                    topic,
                    message.Key,
                    message.Payload,
                    cancellationToken);

                message.MarkProcessed(_timeProvider.GetUtcNow());

                _logger.LogInformation(
                    "Published outbox message {MessageId} of type {MessageType}",
                    message.Id,
                    message.Type);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedKeys.Add(message.Key);
                message.MarkFailed(GetStoredError(exception));

                _logger.LogError(
                    exception,
                    "Failed to publish outbox message {MessageId} of type {MessageType}",
                    message.Id,
                    message.Type);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private string ResolveTopic(string messageType)
    {
        if (_options.Topics.TryGetValue(messageType, out var topic) &&
            !string.IsNullOrWhiteSpace(topic))
        {
            return topic;
        }

        throw new InvalidOperationException(
            $"Kafka topic is not configured for outbox message type '{messageType}'");
    }

    private static string GetStoredError(Exception exception)
    {
        var error = string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;

        return error.Length <= MaxStoredErrorLength
            ? error
            : error[..MaxStoredErrorLength];
    }
}
