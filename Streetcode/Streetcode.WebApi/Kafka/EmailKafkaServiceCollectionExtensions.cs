using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Streetcode.BLL.Interfaces.Email;

namespace Streetcode.WebApi.Kafka;

public static class EmailKafkaServiceCollectionExtensions
{
    public static IServiceCollection AddEmailKafka(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<EmailKafkaOptions>()
            .Bind(configuration.GetRequiredSection(
                EmailKafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IProducer<string, string>>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<EmailKafkaOptions>>()
                    .Value;

                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = options.BootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    AllowAutoCreateTopics = false,
                    ClientId = "streetcode-webapi-email-producer",
                };

                return new ProducerBuilder<string, string>(
                        producerConfig)
                    .Build();
            });

        services.AddSingleton<
            IEmailRequestPublisher,
            KafkaEmailRequestPublisher>();

        return services;
    }
}