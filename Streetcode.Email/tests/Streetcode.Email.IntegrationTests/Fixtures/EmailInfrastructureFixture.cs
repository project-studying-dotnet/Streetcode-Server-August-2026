using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using Testcontainers.MsSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Streetcode.Email.IntegrationTests.Fixtures;

public sealed class EmailInfrastructureFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .WithDatabase("StreetcodeEmail")
            .Build();

    private readonly KafkaContainer _kafkaContainer =
        new KafkaBuilder("confluentinc/cp-kafka:7.5.12")
            .WithKRaft()
            .Build();

    private const string RequestedTopic = "email.requested.v1";
    private const string DeadLetterTopic = "email.requested.v1.dlq";
    private const int MailpitSmtpPort = 1025;
    private const int MailpitHttpPort = 8025;

    public Uri MailpitApiBaseAddress =>
        new UriBuilder(
                Uri.UriSchemeHttp,
                _mailpitContainer.Hostname,
                _mailpitContainer.GetMappedPublicPort(MailpitHttpPort))
            .Uri;

    public string KafkaBootstrapServers =>
        _kafkaContainer.GetBootstrapAddress();

    public string DatabaseConnectionString =>
        _sqlContainer.GetConnectionString();

    public string SmtpHost =>
        _mailpitContainer.Hostname;

    public int SmtpPort =>
        _mailpitContainer.GetMappedPublicPort(
            MailpitSmtpPort);

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _kafkaContainer.StartAsync(),
            _mailpitContainer.StartAsync());

        await CreateKafkaTopicsAsync();
    }

    public async Task DisposeAsync()
    {
        await _mailpitContainer.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    public EmailWebApplicationFactory CreateApplicationFactory(
        string? databaseConnectionString = null)
    {
        return new EmailWebApplicationFactory(
            databaseConnectionString ?? DatabaseConnectionString,
            KafkaBootstrapServers,
            SmtpHost,
            SmtpPort);
    }

    private async Task CreateKafkaTopicsAsync()
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = KafkaBootstrapServers,
        };

        using var adminClient =
            new AdminClientBuilder(config).Build();

        await adminClient.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = RequestedTopic,
                NumPartitions = 3,
                ReplicationFactor = 1,
            },
            new TopicSpecification
            {
                Name = DeadLetterTopic,
                NumPartitions = 3,
                ReplicationFactor = 1,
            },
        ]);
    }

    private readonly IContainer _mailpitContainer =
        new ContainerBuilder("axllent/mailpit:v1.27.4")
            .WithPortBinding(MailpitSmtpPort, true)
            .WithPortBinding(MailpitHttpPort, true)
            .WithEnvironment(
                "MP_SMTP_AUTH_ACCEPT_ANY",
                "1")
            .WithEnvironment(
                "MP_SMTP_AUTH_ALLOW_INSECURE",
                "1")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(
                        MailpitSmtpPort)
                    .UntilInternalTcpPortIsAvailable(
                        MailpitHttpPort))
            .Build();
}
