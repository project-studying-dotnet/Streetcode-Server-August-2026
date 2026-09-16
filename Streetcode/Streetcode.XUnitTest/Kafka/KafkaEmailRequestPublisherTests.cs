using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Streetcode.Email.Contracts.Events;
using Streetcode.WebApi.Kafka;
using Xunit;

namespace Streetcode.XUnitTest.Kafka;

public class KafkaEmailRequestPublisherTests
{
    private const string Topic = "configured.email.topic";

    [Fact]
    public async Task PublishAsync_Persisted_PublishesContractWithoutPiiLog()
    {
        const string sender = "private-sender@example.com";
        const string content = "Private feedback content";
        var emailRequested = CreateEmailRequested(sender, content);
        var producerMock = new Mock<IProducer<string, string>>();
        var logger = new RecordingLogger<KafkaEmailRequestPublisher>();
        string? publishedTopic = null;
        Message<string, string>? publishedMessage = null;

        producerMock
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>(
                (topic, message, _) =>
                {
                    publishedTopic = topic;
                    publishedMessage = message;
                })
            .ReturnsAsync((
                string topic,
                Message<string, string> message,
                CancellationToken _) => CreateDeliveryResult(
                    topic,
                    message,
                    PersistenceStatus.Persisted));

        var publisher = CreatePublisher(producerMock.Object, logger);

        await publisher.PublishAsync(
            emailRequested,
            CancellationToken.None);

        Assert.Equal(Topic, publishedTopic);
        Assert.NotNull(publishedMessage);
        Assert.Equal(
            emailRequested.MessageId.ToString("D"),
            publishedMessage!.Key);

        var serializedContract = JsonSerializer.Deserialize<EmailRequestedV1>(
            publishedMessage.Value);

        Assert.NotNull(serializedContract);
        Assert.Equal(
            emailRequested.MessageId,
            serializedContract!.MessageId);
        Assert.Equal(
            emailRequested.CorrelationId,
            serializedContract.CorrelationId);
        Assert.Equal(
            emailRequested.RequestedAtUtc,
            serializedContract.RequestedAtUtc);
        Assert.Equal(
            emailRequested.Template,
            serializedContract.Template);
        Assert.Equal(
            emailRequested.Recipient,
            serializedContract.Recipient);
        Assert.Equal(
            emailRequested.TemplateData.Count,
            serializedContract.TemplateData.Count);

        foreach (var pair in emailRequested.TemplateData)
        {
            Assert.True(
                serializedContract.TemplateData.TryGetValue(
                    pair.Key,
                    out var value));
            Assert.Equal(pair.Value, value);
        }

        Assert.Single(logger.Messages);
        Assert.DoesNotContain(sender, logger.Messages[0]);
        Assert.DoesNotContain(content, logger.Messages[0]);
    }

    [Fact]
    public async Task PublishAsync_NotPersisted_ThrowsInvalidOperationException()
    {
        var emailRequested = CreateEmailRequested();
        var producerMock = new Mock<IProducer<string, string>>();

        producerMock
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string topic,
                Message<string, string> message,
                CancellationToken _) => CreateDeliveryResult(
                    topic,
                    message,
                    PersistenceStatus.PossiblyPersisted));

        var publisher = CreatePublisher(
            producerMock.Object,
            new RecordingLogger<KafkaEmailRequestPublisher>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(
                emailRequested,
                CancellationToken.None));

        Assert.Contains(
            PersistenceStatus.PossiblyPersisted.ToString(),
            exception.Message);
    }

    [Fact]
    public async Task PublishAsync_ForwardsCancellationToken()
    {
        var emailRequested = CreateEmailRequested();
        var producerMock = new Mock<IProducer<string, string>>();
        var cancellationToken = new CancellationTokenSource().Token;
        var receivedCancellationToken = CancellationToken.None;

        producerMock
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>(
                (_, _, token) => receivedCancellationToken = token)
            .ReturnsAsync((
                string topic,
                Message<string, string> message,
                CancellationToken _) => CreateDeliveryResult(
                    topic,
                    message,
                    PersistenceStatus.Persisted));

        var publisher = CreatePublisher(
            producerMock.Object,
            new RecordingLogger<KafkaEmailRequestPublisher>());

        await publisher.PublishAsync(
            emailRequested,
            cancellationToken);

        Assert.Equal(cancellationToken, receivedCancellationToken);
    }

    private static KafkaEmailRequestPublisher CreatePublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEmailRequestPublisher> logger)
    {
        return new KafkaEmailRequestPublisher(
            producer,
            Options.Create(new EmailKafkaOptions
            {
                BootstrapServers = "localhost:9092",
                Topic = Topic,
            }),
            logger);
    }

    private static EmailRequestedV1 CreateEmailRequested(
        string sender = "sender@example.com",
        string content = "Feedback content")
    {
        return new EmailRequestedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "feedback.v1",
            null,
            new Dictionary<string, string>
            {
                ["From"] = sender,
                ["Content"] = content,
            });
    }

    private static DeliveryResult<string, string> CreateDeliveryResult(
        string topic,
        Message<string, string> message,
        PersistenceStatus status)
    {
        return new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = new Partition(1),
            Offset = new Offset(42),
            Status = status,
            Message = message,
        };
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
