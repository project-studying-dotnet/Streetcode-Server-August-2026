using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Streetcode.Email.Application.EmailTemplates;
using Streetcode.Email.Contracts.Events;
using Streetcode.Email.Domain.EmailDeliveries;
using Streetcode.Email.Infrastructure.Persistence;
using Streetcode.Email.IntegrationTests.Fixtures;

namespace Streetcode.Email.IntegrationTests.EmailDeliveries;

[Collection(EmailInfrastructureCollection.Name)]
public sealed class EmailDeliveryFlowTests
{
    private const string RequestedTopic = "email.requested.v1";
    private const string DeadLetterTopic = "email.requested.v1.dlq";
    private const string ConsumerGroupId =
        "streetcode-email-integration-tests";
    private const string FeedbackRecipient =
        "feedback@streetcode.test";
    private const string FeedbackSubject =
        "Streetcode feedback";

    private static readonly TimeSpan FlowTimeout =
        TimeSpan.FromSeconds(45);

    private static readonly TimeSpan PollInterval =
        TimeSpan.FromMilliseconds(250);

    private readonly EmailInfrastructureFixture _fixture;

    public EmailDeliveryFlowTests(
        EmailInfrastructureFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task ValidRequest_IsPersistedAndSent()
    {
        await using var factory =
            _fixture.CreateApplicationFactory();

        using var applicationClient = factory.CreateClient();
        using var mailpitClient = CreateMailpitClient();

        var initialMessageCount =
            await GetMailpitMessageCountAsync(mailpitClient);

        var emailRequested = CreateEmailRequested();

        await PublishAsync(emailRequested);

        await WaitUntilAsync(
            async cancellationToken =>
            {
                var delivery = await GetDeliveryAsync(
                    emailRequested.MessageId,
                    cancellationToken);

                return delivery?.Status == EmailDeliveryStatus.Sent;
            },
            $"Email delivery '{emailRequested.MessageId}' was not sent.");

        await WaitUntilAsync(
            async cancellationToken =>
                await GetMailpitMessageCountAsync(
                    mailpitClient,
                    cancellationToken) == initialMessageCount + 1,
            "Mailpit did not receive the email.");

        var persistedDelivery = await GetDeliveryAsync(
            emailRequested.MessageId,
            CancellationToken.None);

        Assert.NotNull(persistedDelivery);
        Assert.Equal(
            emailRequested.CorrelationId,
            persistedDelivery!.CorrelationId);
        Assert.Equal(
            EmailDeliveryStatus.Sent,
            persistedDelivery.Status);
        Assert.True(persistedDelivery.IsJobScheduled);

        var mailpitMessage = await GetLatestMailpitMessageAsync(
            mailpitClient);

        Assert.Equal(FeedbackSubject, mailpitMessage.Subject);
        Assert.Contains(
            FeedbackRecipient,
            mailpitMessage.To.Select(address => address.Address));
        Assert.Contains(
            emailRequested.TemplateData[
                FeedbackEmailTemplatePolicy.ContentKey],
            mailpitMessage.Html,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepeatedSentRequest_DoesNotSendAnotherEmail()
    {
        await using var factory =
            _fixture.CreateApplicationFactory();

        using var applicationClient = factory.CreateClient();
        using var mailpitClient = CreateMailpitClient();

        var initialMessageCount =
            await GetMailpitMessageCountAsync(mailpitClient);

        var emailRequested = CreateEmailRequested();

        await PublishAsync(emailRequested);

        await WaitUntilAsync(
            async cancellationToken =>
            {
                var delivery = await GetDeliveryAsync(
                    emailRequested.MessageId,
                    cancellationToken);

                return delivery?.Status == EmailDeliveryStatus.Sent;
            },
            $"Email delivery '{emailRequested.MessageId}' was not sent.");

        await WaitUntilAsync(
            async cancellationToken =>
                await GetMailpitMessageCountAsync(
                    mailpitClient,
                    cancellationToken) == initialMessageCount + 1,
            "Mailpit did not receive the first email.");

        var repeatedDeliveryResult =
            await PublishAsync(emailRequested);

        await WaitUntilCommittedAsync(repeatedDeliveryResult);

        var finalMessageCount =
            await GetMailpitMessageCountAsync(mailpitClient);

        var deliveryRows = await CountDeliveriesAsync(
            emailRequested.MessageId,
            CancellationToken.None);

        Assert.Equal(initialMessageCount + 1, finalMessageCount);
        Assert.Equal(1, deliveryRows);
    }

    [Fact]
    public async Task InvalidJson_IsMovedToDeadLetterTopic()
    {
        await using var factory =
            _fixture.CreateApplicationFactory();

        using var applicationClient = factory.CreateClient();
        using var deadLetterConsumer = CreateDeadLetterConsumer();

        deadLetterConsumer.Subscribe(DeadLetterTopic);

        var messageId = Guid.NewGuid();

        var sourceDeliveryResult = await PublishRawAsync(
            messageId.ToString(),
            "not-valid-json");

        var deadLetterResult = WaitForDeadLetterMessage(
            deadLetterConsumer,
            messageId.ToString());

        await WaitUntilCommittedAsync(sourceDeliveryResult);

        var reasonHeader = deadLetterResult.Message.Headers
            .LastOrDefault(header =>
                header.Key == "dead-letter-reason");

        Assert.NotNull(reasonHeader);
        Assert.Equal(
            "invalid_json",
            Encoding.UTF8.GetString(
                reasonHeader!.GetValueBytes()));
        Assert.Equal("not-valid-json", deadLetterResult.Message.Value);

        var delivery = await GetDeliveryAsync(
            messageId,
            CancellationToken.None);

        Assert.Null(delivery);
    }

    private static EmailRequestedV1 CreateEmailRequested()
    {
        var messageId = Guid.NewGuid();

        return new EmailRequestedV1(
            messageId,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            FeedbackEmailTemplatePolicy.TemplateName,
            null,
            new Dictionary<string, string>
            {
                [FeedbackEmailTemplatePolicy.SenderEmailKey] =
                    $"integration-{messageId:N}@example.com",
                [FeedbackEmailTemplatePolicy.ContentKey] =
                    $"Integration email flow {messageId:N}",
            });
    }

    private async Task<DeliveryResult<string, string>> PublishAsync(
        EmailRequestedV1 emailRequested)
    {
        var value = JsonSerializer.Serialize(emailRequested);

        return await PublishRawAsync(
            emailRequested.MessageId.ToString(),
            value);
    }

    private async Task<DeliveryResult<string, string>> PublishRawAsync(
        string key,
        string value)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
        };

        using var producer =
            new ProducerBuilder<string, string>(config).Build();

        var deliveryResult = await producer.ProduceAsync(
            RequestedTopic,
            new Message<string, string>
            {
                Key = key,
                Value = value,
            });

        Assert.Equal(
            PersistenceStatus.Persisted,
            deliveryResult.Status);

        return deliveryResult;
    }

    private IConsumer<string, string> CreateDeadLetterConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
            GroupId = $"email-dlq-integration-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }

    private static ConsumeResult<string, string> WaitForDeadLetterMessage(
        IConsumer<string, string> consumer,
        string expectedKey)
    {
        var deadline = DateTimeOffset.UtcNow + FlowTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = consumer.Consume(PollInterval);

            if (result?.Message.Key == expectedKey)
            {
                return result;
            }
        }

        throw new TimeoutException(
            $"DLQ message with key '{expectedKey}' was not received.");
    }

    private async Task WaitUntilCommittedAsync(
        DeliveryResult<string, string> deliveryResult)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
            GroupId = ConsumerGroupId,
            EnableAutoCommit = false,
        };

        using var consumer =
            new ConsumerBuilder<Ignore, Ignore>(config).Build();

        await WaitUntilAsync(
            _ => Task.FromResult(
                IsCommitted(consumer, deliveryResult)),
            $"Kafka offset {deliveryResult.Offset.Value} " +
            "was not committed.");
    }

    private static bool IsCommitted(
        IConsumer<Ignore, Ignore> consumer,
        DeliveryResult<string, string> deliveryResult)
    {
        try
        {
            var committed = consumer.Committed(
                [deliveryResult.TopicPartition],
                TimeSpan.FromSeconds(1));

            var offset = committed.Single().Offset;

            return offset != Offset.Unset &&
                   offset.Value > deliveryResult.Offset.Value;
        }
        catch (KafkaException)
        {
            return false;
        }
    }

    private HttpClient CreateMailpitClient()
    {
        return new HttpClient
        {
            BaseAddress = _fixture.MailpitApiBaseAddress,
        };
    }

    private static async Task<long> GetMailpitMessageCountAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var summary = await client.GetFromJsonAsync<
            MailpitMessagesSummary>(
            "api/v1/messages?limit=1",
            cancellationToken);

        return summary?.MessagesCount
               ?? throw new InvalidOperationException(
                   "Mailpit returned an empty messages summary.");
    }

    private static async Task<MailpitMessage> GetLatestMailpitMessageAsync(
        HttpClient client)
    {
        var message = await client.GetFromJsonAsync<MailpitMessage>(
            "api/v1/message/latest");

        return message
               ?? throw new InvalidOperationException(
                   "Mailpit returned an empty message.");
    }

    private async Task<EmailDelivery?> GetDeliveryAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.EmailDeliveries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                delivery => delivery.MessageId == messageId,
                cancellationToken);
    }

    private async Task<int> CountDeliveriesAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.EmailDeliveries
            .AsNoTracking()
            .CountAsync(
                delivery => delivery.MessageId == messageId,
                cancellationToken);
    }

    private EmailDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<EmailDbContext>()
                .UseSqlServer(_fixture.DatabaseConnectionString)
                .Options;

        return new EmailDbContext(options);
    }

    private static async Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> condition,
        string timeoutMessage)
    {
        using var timeout =
            new CancellationTokenSource(FlowTimeout);

        try
        {
            while (!await condition(timeout.Token))
            {
                await Task.Delay(
                    PollInterval,
                    timeout.Token);
            }
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(timeoutMessage);
        }
    }

    private sealed record MailpitMessagesSummary(
        [property: JsonPropertyName("messages_count")]
        long MessagesCount);

    private sealed record MailpitMessage(
        [property: JsonPropertyName("Subject")]
        string Subject,
        [property: JsonPropertyName("HTML")]
        string Html,
        [property: JsonPropertyName("To")]
        IReadOnlyList<MailpitAddress> To);

    private sealed record MailpitAddress(
        [property: JsonPropertyName("Address")]
        string Address);
}
