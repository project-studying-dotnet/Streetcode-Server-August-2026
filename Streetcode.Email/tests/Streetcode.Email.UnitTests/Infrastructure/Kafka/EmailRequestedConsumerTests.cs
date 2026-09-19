using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Application.EmailRequests;
using Streetcode.Email.Contracts.Events;
using Streetcode.Email.Domain.EmailDeliveries;
using Streetcode.Email.Infrastructure.Kafka;

namespace Streetcode.Email.UnitTests.Infrastructure.Kafka;

public sealed class EmailRequestedConsumerTests
{
    private const string Topic = "email.requested.v1";

    [Fact]
    public void Constructor_WithNullConsumerFactory_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new EmailRequestedConsumer(
                CreateOptions(),
                null!,
                new Mock<IServiceScopeFactory>().Object,
                NullLogger<EmailRequestedConsumer>.Instance,
                new Mock<IEmailDeadLetterPublisher>().Object));

        Assert.Equal("consumerFactory", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_PublishesToDlqBeforeCommit()
    {
        var calls = new List<string>();
        var consumeResult = CreateConsumeResult("invalid-json");
        var commitObserved = CreateCompletionSource();
        var kafkaConsumer = CreateKafkaConsumer(
            consumeResult,
            commitObserved,
            () => calls.Add("commit"));
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        deadLetterPublisher
            .Setup(publisher => publisher.PublishAsync(
                consumeResult,
                "invalid_json",
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("publish"))
            .Returns(YieldOnceAsync);
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object);

        await RunUntilCommitAsync(service, commitObserved.Task);

        Assert.Equal(new[] { "publish", "commit" }, calls);
        kafkaConsumer.Verify(
            consumer => consumer.Subscribe(Topic),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Close(),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Dispose(),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidMessage_EnqueuesAndCommits()
    {
        var emailRequested = CreateEmailRequested();
        var consumeResult = CreateConsumeResult(
            JsonSerializer.Serialize(emailRequested),
            emailRequested.MessageId.ToString());
        var commitObserved = CreateCompletionSource();
        var kafkaConsumer = CreateKafkaConsumer(
            consumeResult,
            commitObserved);
        var repository = new Mock<IEmailDeliveryRepository>();
        repository
            .Setup(value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailDelivery?)null);
        var scheduler = new Mock<IEmailJobScheduler>();
        scheduler
            .Setup(value => value.EnqueueAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .Returns(YieldOnceAsync);
        using var services = CreateHandlerServices(
            repository.Object,
            scheduler.Object);
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object,
            services.GetRequiredService<IServiceScopeFactory>());

        await RunUntilCommitAsync(service, commitObserved.Task);

        repository.Verify(
            value => value.AddAsync(
                It.Is<EmailDelivery>(delivery =>
                    delivery.MessageId == emailRequested.MessageId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            value => value.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        scheduler.Verify(
            value => value.EnqueueAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        deadLetterPublisher.VerifyNoOtherCalls();
        kafkaConsumer.Verify(
            consumer => consumer.Commit(consumeResult),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenKafkaCommitInitiallyFails_RetriesWithoutSecondJob()
    {
        var emailRequested = CreateEmailRequested();
        var consumeResult = CreateConsumeResult(
            JsonSerializer.Serialize(emailRequested),
            emailRequested.MessageId.ToString());
        var commitObserved = CreateCompletionSource();
        var consumeCalls = 0;
        var commitCalls = 0;
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(consumer => consumer.Consume(
                It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref consumeCalls) <= 2)
                {
                    return consumeResult;
                }

                cancellationToken.WaitHandle.WaitOne();
                throw new OperationCanceledException(cancellationToken);
            });
        kafkaConsumer
            .Setup(consumer => consumer.Commit(consumeResult))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref commitCalls) == 1)
                {
                    throw new KafkaException(
                        new Error(ErrorCode.Local_AllBrokersDown));
                }

                commitObserved.TrySetResult(true);
            });

        EmailDelivery? storedDelivery = null;
        var repository = new Mock<IEmailDeliveryRepository>();
        repository
            .Setup(value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(storedDelivery));
        repository
            .Setup(value => value.AddAsync(
                It.IsAny<EmailDelivery>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailDelivery, CancellationToken>(
                (delivery, _) => storedDelivery = delivery)
            .Returns(Task.CompletedTask);
        repository
            .Setup(value => value.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var scheduler = new Mock<IEmailJobScheduler>();
        scheduler
            .Setup(value => value.EnqueueAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .Returns(YieldOnceAsync);
        using var services = CreateHandlerServices(
            repository.Object,
            scheduler.Object);
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object,
            services.GetRequiredService<IServiceScopeFactory>());

        await RunUntilCommitAsync(service, commitObserved.Task);

        Assert.NotNull(storedDelivery);
        Assert.True(storedDelivery.IsJobScheduled);
        scheduler.Verify(
            value => value.EnqueueAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Seek(
                consumeResult.TopicPartitionOffset),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Commit(consumeResult),
            Times.Exactly(2));
        deadLetterPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDlqPublishInitiallyFails_SeeksAndRetriesBeforeCommit()
    {
        var calls = new List<string>();
        var consumeResult = CreateConsumeResult("invalid-json");
        var commitObserved = CreateCompletionSource();
        var consumeCalls = 0;
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(consumer => consumer.Consume(
                It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref consumeCalls) <= 2)
                {
                    return consumeResult;
                }

                cancellationToken.WaitHandle.WaitOne();
                throw new OperationCanceledException(cancellationToken);
            });
        kafkaConsumer
            .Setup(consumer => consumer.Seek(
                consumeResult.TopicPartitionOffset))
            .Callback(() => calls.Add("seek"));
        kafkaConsumer
            .Setup(consumer => consumer.Commit(consumeResult))
            .Callback(() =>
            {
                calls.Add("commit");
                commitObserved.TrySetResult(true);
            });
        var expectedException = new InvalidOperationException(
            "DLQ is unavailable.");
        var publishAttempts = 0;
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        deadLetterPublisher
            .Setup(publisher => publisher.PublishAsync(
                consumeResult,
                "invalid_json",
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                calls.Add("publish");

                return Interlocked.Increment(ref publishAttempts) == 1
                    ? Task.FromException(expectedException)
                    : YieldOnceAsync();
            });
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object);

        await RunUntilCommitAsync(service, commitObserved.Task);

        Assert.Equal(
            new[] { "publish", "seek", "publish", "commit" },
            calls);
        deadLetterPublisher.Verify(
            publisher => publisher.PublishAsync(
                consumeResult,
                "invalid_json",
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        kafkaConsumer.Verify(
            consumer => consumer.Seek(
                consumeResult.TopicPartitionOffset),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Commit(consumeResult),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingRetriesAreExhausted_PublishesToDlqAndCommits()
    {
        var emailRequested = CreateEmailRequested();
        var consumeResult = CreateConsumeResult(
            JsonSerializer.Serialize(emailRequested),
            emailRequested.MessageId.ToString());
        var commitObserved = CreateCompletionSource();
        var kafkaConsumer = CreateKafkaConsumer(
            consumeResult,
            commitObserved);
        var repository = new Mock<IEmailDeliveryRepository>();
        repository
            .Setup(value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "Email database is temporarily unavailable.",
                new TransientDbException()));
        var scheduler = new Mock<IEmailJobScheduler>();
        using var services = CreateHandlerServices(
            repository.Object,
            scheduler.Object);
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        deadLetterPublisher
            .Setup(publisher => publisher.PublishAsync(
                consumeResult,
                "processing_retries_exhausted",
                It.IsAny<CancellationToken>()))
            .Returns(YieldOnceAsync);
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object,
            services.GetRequiredService<IServiceScopeFactory>(),
            maxProcessingAttempts: 1);

        await RunUntilCommitAsync(service, commitObserved.Task);

        deadLetterPublisher.Verify(
            publisher => publisher.PublishAsync(
                consumeResult,
                "processing_retries_exhausted",
                It.IsAny<CancellationToken>()),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Commit(consumeResult),
            Times.Once);
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WithPermanentDbUpdateFailure_DoesNotRetryProcessing()
    {
        var emailRequested = CreateEmailRequested();
        var consumeResult = CreateConsumeResult(
            JsonSerializer.Serialize(emailRequested),
            emailRequested.MessageId.ToString());
        var commitObserved = CreateCompletionSource();
        var kafkaConsumer = CreateKafkaConsumer(
            consumeResult,
            commitObserved);
        var repository = new Mock<IEmailDeliveryRepository>();
        repository
            .Setup(value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "The request violates a database constraint."));
        var scheduler = new Mock<IEmailJobScheduler>();
        using var services = CreateHandlerServices(
            repository.Object,
            scheduler.Object);
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        deadLetterPublisher
            .Setup(publisher => publisher.PublishAsync(
                consumeResult,
                "unexpected_processing_error",
                It.IsAny<CancellationToken>()))
            .Returns(YieldOnceAsync);
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object,
            services.GetRequiredService<IServiceScopeFactory>());

        await RunUntilCommitAsync(service, commitObserved.Task);

        repository.Verify(
            value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        deadLetterPublisher.Verify(
            publisher => publisher.PublishAsync(
                consumeResult,
                "unexpected_processing_error",
                It.IsAny<CancellationToken>()),
            Times.Once);
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedProcessingFailureOccurs_PublishesToDlqAndCommits()
    {
        var emailRequested = CreateEmailRequested();
        var consumeResult = CreateConsumeResult(
            JsonSerializer.Serialize(emailRequested),
            emailRequested.MessageId.ToString());
        var commitObserved = CreateCompletionSource();
        var kafkaConsumer = CreateKafkaConsumer(
            consumeResult,
            commitObserved);
        var repository = new Mock<IEmailDeliveryRepository>();
        repository
            .Setup(value => value.GetByMessageIdAsync(
                emailRequested.MessageId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Unexpected processing failure."));
        var scheduler = new Mock<IEmailJobScheduler>();
        using var services = CreateHandlerServices(
            repository.Object,
            scheduler.Object);
        var deadLetterPublisher = new Mock<IEmailDeadLetterPublisher>();
        deadLetterPublisher
            .Setup(publisher => publisher.PublishAsync(
                consumeResult,
                "unexpected_processing_error",
                It.IsAny<CancellationToken>()))
            .Returns(YieldOnceAsync);
        var service = CreateConsumerService(
            kafkaConsumer.Object,
            deadLetterPublisher.Object,
            services.GetRequiredService<IServiceScopeFactory>());

        await RunUntilCommitAsync(service, commitObserved.Task);

        deadLetterPublisher.Verify(
            publisher => publisher.PublishAsync(
                consumeResult,
                "unexpected_processing_error",
                It.IsAny<CancellationToken>()),
            Times.Once);
        kafkaConsumer.Verify(
            consumer => consumer.Commit(consumeResult),
            Times.Once);
        scheduler.VerifyNoOtherCalls();
    }

    private static EmailRequestedConsumer CreateConsumerService(
        IConsumer<string, string> kafkaConsumer,
        IEmailDeadLetterPublisher deadLetterPublisher,
        IServiceScopeFactory? scopeFactory = null,
        int maxProcessingAttempts = 3)
    {
        var consumerFactory = new Mock<IEmailRequestedConsumerFactory>();
        consumerFactory
            .Setup(factory => factory.Create())
            .Returns(kafkaConsumer);

        return new EmailRequestedConsumer(
            CreateOptions(maxProcessingAttempts),
            consumerFactory.Object,
            scopeFactory ?? new Mock<IServiceScopeFactory>().Object,
            NullLogger<EmailRequestedConsumer>.Instance,
            deadLetterPublisher);
    }

    private static ServiceProvider CreateHandlerServices(
        IEmailDeliveryRepository repository,
        IEmailJobScheduler scheduler)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new RequestEmailDeliveryCommandHandler(
            new RequestEmailDeliveryCommandValidator(),
            repository,
            scheduler));

        return services.BuildServiceProvider();
    }

    private static Mock<IConsumer<string, string>> CreateKafkaConsumer(
        ConsumeResult<string, string> consumeResult,
        TaskCompletionSource<bool> commitObserved,
        Action? onCommit = null)
    {
        var consumeCalls = 0;
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(consumer => consumer.Consume(
                It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref consumeCalls) == 1)
                {
                    return consumeResult;
                }

                cancellationToken.WaitHandle.WaitOne();
                throw new OperationCanceledException(cancellationToken);
            });
        kafkaConsumer
            .Setup(consumer => consumer.Commit(consumeResult))
            .Callback(() =>
            {
                onCommit?.Invoke();
                commitObserved.TrySetResult(true);
            });

        return kafkaConsumer;
    }

    private static async Task RunUntilCommitAsync(
        EmailRequestedConsumer service,
        Task commitObserved)
    {
        await service.StartAsync(CancellationToken.None);

        try
        {
            await commitObserved.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            service.Dispose();
        }
    }

    private static IOptions<KafkaOptions> CreateOptions(
        int maxProcessingAttempts = 3)
    {
        return Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            GroupId = "streetcode-email-service-v1",
            Topic = Topic,
            DeadLetterTopic = "email.requested.v1.dlq",
            MaxProcessingAttempts = maxProcessingAttempts,
            RetryDelayMilliseconds = 100,
        });
    }

    private static EmailRequestedV1 CreateEmailRequested()
    {
        return new EmailRequestedV1(
            Guid.Parse("2fa56b12-4859-43b5-8ced-cb7ccf535fc2"),
            Guid.Parse("0190c271-1794-7a93-9ed8-6f93b832b1f7"),
            new DateTimeOffset(
                2026,
                9,
                12,
                9,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>
            {
                ["From"] = "sender@example.com",
                ["Content"] = "Feedback message",
            });
    }

    private static ConsumeResult<string, string> CreateConsumeResult(
        string value,
        string? key = null)
    {
        return new ConsumeResult<string, string>
        {
            Topic = Topic,
            Partition = new Partition(1),
            Offset = new Offset(42),
            Message = new Message<string, string>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = value,
            },
        };
    }

    private static TaskCompletionSource<bool> CreateCompletionSource()
    {
        return new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static async Task YieldOnceAsync()
    {
        await Task.Yield();
    }

    private sealed class TransientDbException : System.Data.Common.DbException
    {
        public override bool IsTransient => true;
    }
}
