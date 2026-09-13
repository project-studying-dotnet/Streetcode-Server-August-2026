using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Streetcode.Email.Infrastructure.Kafka;

namespace Streetcode.Email.UnitTests.Infrastructure.Kafka;

public sealed class EmailDeadLetterPublisherTests
{
    private const string DeadLetterTopic = "email.requested.v1.dlq";

    [Fact]
    public void Constructor_WithNullProducer_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new EmailDeadLetterPublisher(
                null!,
                CreateOptions(),
                NullLogger<EmailDeadLetterPublisher>.Instance));

        Assert.Equal("producer", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var producer = new RecordingProducer();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new EmailDeadLetterPublisher(
                producer,
                null!,
                NullLogger<EmailDeadLetterPublisher>.Instance));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var producer = new RecordingProducer();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new EmailDeadLetterPublisher(
                producer,
                CreateOptions(),
                null!));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_WithNullConsumeResult_ThrowsArgumentNullException()
    {
        var publisher = CreatePublisher(new RecordingProducer());

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishAsync(
                null!,
                "invalid_json",
                CancellationToken.None));

        Assert.Equal("consumeResult", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PublishAsync_WithInvalidReasonCode_ThrowsArgumentException(
        string? reasonCode)
    {
        var producer = new RecordingProducer();
        var publisher = CreatePublisher(producer);

        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => publisher.PublishAsync(
                CreateConsumeResult(),
                reasonCode!,
                CancellationToken.None));

        Assert.Equal("reasonCode", exception.ParamName);
        Assert.Null(producer.ProducedMessage);
    }

    [Fact]
    public async Task PublishAsync_WithPersistedDelivery_PublishesExpectedMessage()
    {
        const string reasonCode = "invalid_json";
        var producer = new RecordingProducer();
        var publisher = CreatePublisher(producer);
        var consumeResult = CreateConsumeResult();
        using var cancellationTokenSource = new CancellationTokenSource();

        await publisher.PublishAsync(
            consumeResult,
            reasonCode,
            cancellationTokenSource.Token);

        Assert.Equal(DeadLetterTopic, producer.ProducedTopic);
        Assert.Equal(
            cancellationTokenSource.Token,
            producer.ReceivedCancellationToken);

        var producedMessage = Assert.IsType<Message<string, string>>(
            producer.ProducedMessage);
        Assert.Equal(consumeResult.Message.Key, producedMessage.Key);
        Assert.Equal(consumeResult.Message.Value, producedMessage.Value);
        Assert.Equal(
            consumeResult.Topic,
            GetHeaderValue(producedMessage.Headers, "source-topic"));
        Assert.Equal(
            consumeResult.Partition.Value.ToString(),
            GetHeaderValue(producedMessage.Headers, "source-partition"));
        Assert.Equal(
            consumeResult.Offset.Value.ToString(),
            GetHeaderValue(producedMessage.Headers, "source-offset"));
        Assert.Equal(
            reasonCode,
            GetHeaderValue(producedMessage.Headers, "dead-letter-reason"));
    }

    [Fact]
    public async Task PublishAsync_WhenDeliveryIsNotPersisted_ThrowsInvalidOperationException()
    {
        var producer = new RecordingProducer
        {
            DeliveryStatus = PersistenceStatus.PossiblyPersisted,
        };
        var publisher = CreatePublisher(producer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(
                CreateConsumeResult(),
                "invalid_json",
                CancellationToken.None));

        Assert.Equal(
            "Kafka did not persist the DLQ message. " +
            "Status: PossiblyPersisted.",
            exception.Message);
        Assert.NotNull(producer.ProducedMessage);
    }

    [Fact]
    public async Task PublishAsync_WhenProducerFails_PropagatesException()
    {
        var expectedException = new InvalidOperationException(
            "Kafka is unavailable.");
        var producer = new RecordingProducer
        {
            ProduceException = expectedException,
        };
        var publisher = CreatePublisher(producer);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(
                CreateConsumeResult(),
                "invalid_json",
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }

    private static EmailDeadLetterPublisher CreatePublisher(
        IProducer<string, string> producer)
    {
        return new EmailDeadLetterPublisher(
            producer,
            CreateOptions(),
            NullLogger<EmailDeadLetterPublisher>.Instance);
    }

    private static IOptions<KafkaOptions> CreateOptions()
    {
        return Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            GroupId = "streetcode-email-service-v1",
            Topic = "email.requested.v1",
            DeadLetterTopic = DeadLetterTopic,
        });
    }

    private static ConsumeResult<string, string> CreateConsumeResult()
    {
        return new ConsumeResult<string, string>
        {
            Topic = "email.requested.v1",
            Partition = new Partition(2),
            Offset = new Offset(42),
            Message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = "{\"template\":\"feedback.v1\"}",
            },
        };
    }

    private static string GetHeaderValue(
        Headers headers,
        string key)
    {
        var bytes = headers.GetLastBytes(key);
        Assert.NotNull(bytes);

        return Encoding.UTF8.GetString(bytes);
    }

    private sealed class RecordingProducer : IProducer<string, string>
    {
        public PersistenceStatus DeliveryStatus { get; init; } =
            PersistenceStatus.Persisted;

        public Exception? ProduceException { get; init; }

        public string? ProducedTopic { get; private set; }

        public Message<string, string>? ProducedMessage { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Handle Handle => null!;

        public string Name => nameof(RecordingProducer);

        public Task<DeliveryResult<string, string>> ProduceAsync(
            string topic,
            Message<string, string> message,
            CancellationToken cancellationToken = default)
        {
            ProducedTopic = topic;
            ProducedMessage = message;
            ReceivedCancellationToken = cancellationToken;

            if (ProduceException is not null)
            {
                return Task.FromException<DeliveryResult<string, string>>(
                    ProduceException);
            }

            return Task.FromResult(new DeliveryResult<string, string>
            {
                Topic = topic,
                Partition = new Partition(0),
                Offset = new Offset(1),
                Status = DeliveryStatus,
                Message = message,
            });
        }

        public Task<DeliveryResult<string, string>> ProduceAsync(
            TopicPartition topicPartition,
            Message<string, string> message,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Produce(
            string topic,
            Message<string, string> message,
            Action<DeliveryReport<string, string>>? deliveryHandler = null)
        {
            throw new NotSupportedException();
        }

        public void Produce(
            TopicPartition topicPartition,
            Message<string, string> message,
            Action<DeliveryReport<string, string>>? deliveryHandler = null)
        {
            throw new NotSupportedException();
        }

        public int Poll(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public int Flush(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void Flush(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void InitTransactions(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void BeginTransaction()
        {
            throw new NotSupportedException();
        }

        public void CommitTransaction(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void CommitTransaction()
        {
            throw new NotSupportedException();
        }

        public void AbortTransaction(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void AbortTransaction()
        {
            throw new NotSupportedException();
        }

        public void SendOffsetsToTransaction(
            IEnumerable<TopicPartitionOffset> offsets,
            IConsumerGroupMetadata groupMetadata,
            TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public int AddBrokers(string brokers)
        {
            throw new NotSupportedException();
        }

        public void SetSaslCredentials(
            string username,
            string password)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
