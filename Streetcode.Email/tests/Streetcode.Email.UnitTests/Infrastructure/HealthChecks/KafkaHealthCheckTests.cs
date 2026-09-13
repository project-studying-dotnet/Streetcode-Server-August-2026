using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Streetcode.Email.Infrastructure.HealthChecks;
using Streetcode.Email.Infrastructure.Kafka;

namespace Streetcode.Email.UnitTests.Infrastructure.HealthChecks;

public sealed class KafkaHealthCheckTests
{
    private const string Topic = "email.requested.v1";
    private static readonly TimeSpan ExpectedTimeout =
        TimeSpan.FromSeconds(5);

    [Fact]
    public void Constructor_WithNullAdminClient_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new KafkaHealthCheck(
                null!,
                CreateOptions()));

        Assert.Equal("adminClient", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var adminClient = new Mock<IAdminClient>();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new KafkaHealthCheck(
                adminClient.Object,
                null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task CheckHealthAsync_WithAvailableBrokerAndTopic_ReturnsHealthy()
    {
        var metadata = CreateMetadata();
        var adminClient = CreateAdminClient(metadata);
        var healthCheck = CreateHealthCheck(adminClient.Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        adminClient.Verify(
            client => client.GetMetadata(Topic, ExpectedTimeout),
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoBrokers_ReturnsUnhealthy()
    {
        var metadata = CreateMetadata(includeBroker: false);
        var healthCheck = CreateHealthCheck(
            CreateAdminClient(metadata).Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithMissingTopic_ReturnsUnhealthy()
    {
        var metadata = CreateMetadata(includeTopic: false);
        var healthCheck = CreateHealthCheck(
            CreateAdminClient(metadata).Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTopicError_ReturnsUnhealthy()
    {
        var metadata = CreateMetadata(
            topicError: new Error(ErrorCode.UnknownTopicOrPart));
        var healthCheck = CreateHealthCheck(
            CreateAdminClient(metadata).Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoPartitions_ReturnsUnhealthy()
    {
        var metadata = CreateMetadata(includePartition: false);
        var healthCheck = CreateHealthCheck(
            CreateAdminClient(metadata).Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenKafkaThrows_ReturnsUnhealthyWithException()
    {
        var kafkaException = new KafkaException(
            new Error(ErrorCode.Local_AllBrokersDown));
        var adminClient = new Mock<IAdminClient>();
        adminClient
            .Setup(client => client.GetMetadata(Topic, ExpectedTimeout))
            .Throws(kafkaException);
        var healthCheck = CreateHealthCheck(adminClient.Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Same(kafkaException, result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var adminClient = new Mock<IAdminClient>();
        var healthCheck = CreateHealthCheck(adminClient.Object);
        using var cancellationTokenSource =
            new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => healthCheck.CheckHealthAsync(
                new HealthCheckContext(),
                cancellationTokenSource.Token));

        adminClient.Verify(
            client => client.GetMetadata(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()),
            Times.Never);
    }

    private static KafkaHealthCheck CreateHealthCheck(
        IAdminClient adminClient)
    {
        return new KafkaHealthCheck(
            adminClient,
            CreateOptions());
    }

    private static IOptions<KafkaOptions> CreateOptions()
    {
        return Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            GroupId = "streetcode-email-service-v1",
            Topic = Topic,
            DeadLetterTopic = "email.requested.v1.dlq",
        });
    }

    private static Mock<IAdminClient> CreateAdminClient(
        Metadata metadata)
    {
        var adminClient = new Mock<IAdminClient>();
        adminClient
            .Setup(client => client.GetMetadata(Topic, ExpectedTimeout))
            .Returns(metadata);

        return adminClient;
    }

    private static Metadata CreateMetadata(
        bool includeBroker = true,
        bool includeTopic = true,
        bool includePartition = true,
        Error? topicError = null)
    {
        var brokers = includeBroker
            ? new List<BrokerMetadata>
            {
                new(1, "localhost", 9092),
            }
            : [];

        var partitions = includePartition
            ? new List<PartitionMetadata>
            {
                new(
                    0,
                    1,
                    [1],
                    [1],
                    new Error(ErrorCode.NoError)),
            }
            : [];

        var topics = includeTopic
            ? new List<TopicMetadata>
            {
                new(
                    Topic,
                    partitions,
                    topicError ?? new Error(ErrorCode.NoError)),
            }
            : [];

        return new Metadata(
            brokers,
            topics,
            1,
            "localhost:9092");
    }
}
