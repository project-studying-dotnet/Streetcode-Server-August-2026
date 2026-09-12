using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.Infrastructure.Messaging.Kafka;

public static class KafkaDependencyInjection
{
    public static IServiceCollection AddKafkaMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
                "Kafka BootstrapServers must be configured")
            .Validate(
                options =>
                    options.Topics is not null &&
                    options.Topics.TryGetValue(nameof(UserAccessChangedV1), out var topic) &&
                    !string.IsNullOrWhiteSpace(topic),
                $"Kafka topic for {nameof(UserAccessChangedV1)} must be configured")
            .Validate(
                options => options.OutboxBatchSize is > 0 and <= 500,
                "Kafka OutboxBatchSize must be between 1 and 500")
            .Validate(
                options =>
                    options.OutboxPollingInterval > TimeSpan.Zero &&
                    options.OutboxPollingInterval <= TimeSpan.FromMinutes(5),
                "Kafka OutboxPollingInterval must be between zero and five minutes")
            .ValidateOnStart();

        services.AddSingleton<IProducer<string, string>>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<KafkaOptions>>()
                .Value;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                EnableIdempotence = true,
                Acks = Acks.All,
                AllowAutoCreateTopics = false,
            };

            return new ProducerBuilder<string, string>(
                producerConfig).Build();
        });

        services.AddSingleton<
            IKafkaMessageProducer,
            KafkaMessageProducer>();

        services.AddScoped<OutboxPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
