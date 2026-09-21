using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Streetcode.Email.IntegrationTests.Fixtures;

public sealed class EmailWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseConnectionString;
    private readonly string _kafkaBootstrapServers;

    private readonly string _smtpHost;
    private readonly int _smtpPort;

    public EmailWebApplicationFactory(
        string databaseConnectionString,
        string kafkaBootstrapServers,
        string smtpHost,
        int smtpPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            databaseConnectionString);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            kafkaBootstrapServers);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            smtpHost);

        if (smtpPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(smtpPort));
        }

        _databaseConnectionString = databaseConnectionString;
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EmailDatabase"] =
                        _databaseConnectionString,
                });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:EmailDatabase"] =
                            _databaseConnectionString,

                        ["Kafka:BootstrapServers"] =
                            _kafkaBootstrapServers,

                        ["Kafka:GroupId"] =
                            "streetcode-email-integration-tests",

                        ["Kafka:Topic"] =
                            "email.requested.v1",

                        ["Kafka:DeadLetterTopic"] =
                            "email.requested.v1.dlq",

                        ["Smtp:Host"] =
                            _smtpHost,

                        ["Smtp:Port"] =
                            _smtpPort.ToString(
                                CultureInfo.InvariantCulture),

                        ["Smtp:UseSsl"] =
                            bool.FalseString,

                        ["Smtp:Username"] =
                            "integration-test",

                        ["Smtp:Password"] =
                            "integration-test",

                        ["Smtp:SenderAddress"] =
                            "no-reply@streetcode.test",

                        ["EmailTemplates:Feedback:RecipientAddress"] =
                            "feedback@streetcode.test",
                    });
            });
    }
}
