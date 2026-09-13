using System.Data.Common;
using System.Text.Json;
using Confluent.Kafka;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.Email.Application.EmailRequests;
using Streetcode.Email.Contracts.Events;

namespace Streetcode.Email.Infrastructure.Kafka;

public sealed class EmailRequestedConsumer : BackgroundService
{
    private const string InvalidJsonReason = "invalid_json";
    private const string InvalidMessageKeyReason = "invalid_message_key";
    private const string ValidationFailedReason = "validation_failed";
    private const string MessageIdConflictReason = "message_id_conflict";
    private const string ProcessingRetriesExhaustedReason =
        "processing_retries_exhausted";

    private readonly IOptions<KafkaOptions> _options;
    private readonly IEmailRequestedConsumerFactory _consumerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailRequestedConsumer> _logger;
    private readonly IEmailDeadLetterPublisher _emailDeadLetterPublisher;

    public EmailRequestedConsumer(
        IOptions<KafkaOptions> options,
        IEmailRequestedConsumerFactory consumerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailRequestedConsumer> logger,
        IEmailDeadLetterPublisher emailDeadLetterPublisher)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(emailDeadLetterPublisher);

        _options = options;
        _consumerFactory = consumerFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _emailDeadLetterPublisher = emailDeadLetterPublisher;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var consumer = _consumerFactory.Create();

        consumer.Subscribe(_options.Value.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    if (result.Message is null ||
                        string.IsNullOrWhiteSpace(
                            result.Message.Value))
                    {
                        throw new JsonException(
                            "Kafka message contains an empty " +
                            "email request.");
                    }

                    var emailRequested =
                        JsonSerializer.Deserialize<EmailRequestedV1>(
                            result.Message.Value)
                        ?? throw new JsonException(
                            "Kafka message contains an empty email request.");

                    if (!Guid.TryParse(
                            result.Message.Key,
                            out var messageId) ||
                        messageId != emailRequested.MessageId)
                    {
                        throw new InvalidDataException(
                            "Kafka key does not match MessageId.");
                    }

                    var command = new RequestEmailDeliveryCommand(
                        emailRequested.MessageId,
                        emailRequested.CorrelationId,
                        emailRequested.RequestedAtUtc,
                        emailRequested.Template,
                        emailRequested.Recipient,
                        emailRequested.TemplateData);

                    await HandleWithRetryAsync(
                        command,
                        result,
                        stoppingToken);

                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Email request {MessageId} was accepted. " +
                        "Kafka topic {Topic}, partition {Partition}, " +
                        "offset {Offset} was committed.",
                        emailRequested.MessageId,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
                }
                catch (JsonException)
                {
                    await MoveToDeadLetterAsync(
                        consumer,
                        result,
                        InvalidJsonReason,
                        stoppingToken);
                }
                catch (InvalidDataException)
                {
                    await MoveToDeadLetterAsync(
                        consumer,
                        result,
                        InvalidMessageKeyReason,
                        stoppingToken);
                }
                catch (ValidationException)
                {
                    await MoveToDeadLetterAsync(
                        consumer,
                        result,
                        ValidationFailedReason,
                        stoppingToken);
                }
                catch (EmailRequestConflictException)
                {
                    await MoveToDeadLetterAsync(
                        consumer,
                        result,
                        MessageIdConflictReason,
                        stoppingToken);
                }
                catch (Exception exception)
                    when (IsTransient(exception))
                {
                    _logger.LogError(
                        exception,
                        "Email request processing exhausted all retry attempts. " +
                        "Kafka topic {Topic}, partition {Partition}, " +
                        "offset {Offset}. Moving message to DLQ.",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);

                    await MoveToDeadLetterAsync(
                        consumer,
                        result,
                        ProcessingRetriesExhaustedReason,
                        stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task MoveToDeadLetterAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await _emailDeadLetterPublisher.PublishAsync(
            result,
            reasonCode,
            cancellationToken);

        consumer.Commit(result);
    }

    private async Task HandleWithRetryAsync(
        RequestEmailDeliveryCommand command,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        var maxAttempts =
            _options.Value.MaxProcessingAttempts;

        var retryDelayMilliseconds =
            _options.Value.RetryDelayMilliseconds;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var handler = scope.ServiceProvider
                    .GetRequiredService<
                        RequestEmailDeliveryCommandHandler>();

                await handler.HandleAsync(
                    command,
                    cancellationToken);

                return;
            }
            catch (Exception exception)
                when (IsTransient(exception) &&
                      attempt < maxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Transient failure while processing email " +
                    "request {MessageId}. Kafka topic {Topic}, " +
                    "partition {Partition}, offset {Offset}. " +
                    "Attempt {Attempt}/{MaxAttempts}. " +
                    "Retrying in {RetryDelayMilliseconds} ms.",
                    command.MessageId,
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    attempt,
                    maxAttempts,
                    retryDelayMilliseconds);

                await Task.Delay(
                    retryDelayMilliseconds,
                    cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception)
    {
        return exception is DbException
            or DbUpdateException
            or BackgroundJobClientException
            or TimeoutException;
    }
}
