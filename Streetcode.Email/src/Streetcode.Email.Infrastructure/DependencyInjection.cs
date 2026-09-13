using Confluent.Kafka;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Infrastructure.BackgroundJobs;
using Streetcode.Email.Infrastructure.EmailSending;
using Streetcode.Email.Infrastructure.Kafka;
using Streetcode.Email.Infrastructure.Persistence;
using Streetcode.Email.Infrastructure.Persistence.Repositories;

namespace Streetcode.Email.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EmailDbContext>(
            options => options.UseSqlServer(connectionString));

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString));

        services.AddHangfireServer();

        services.AddScoped<IEmailDeliveryRepository, EmailDeliveryRepository>();

        services.AddScoped<IEmailJobScheduler, HangfireEmailJobScheduler>();

        services.AddScoped<SendEmailDeliveryJob>();

        services.AddScoped<MarkEmailDeliveryAsFailedJob>();

        services.AddScoped<FeedbackEmailMessageFactory>();

        services.AddScoped<IEmailDeliverySender, MailKitEmailDeliverySender>();

        services.AddSingleton<IAdminClient>(serviceProvider =>
        {
            var kafkaOptions = serviceProvider
                .GetRequiredService<IOptions<KafkaOptions>>()
                .Value;

            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
            };

            return new AdminClientBuilder(
                adminClientConfig).Build();
        });

        services.AddSingleton<IProducer<string, string>>(serviceProvider =>
        {
            var kafkaOptions = serviceProvider
                .GetRequiredService<IOptions<KafkaOptions>>()
                .Value;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
            };

            return new ProducerBuilder<string, string>(
                producerConfig).Build();
        });

        services.AddSingleton<
            IEmailDeadLetterPublisher,
            EmailDeadLetterPublisher>();

        services.AddSingleton<
            IEmailRequestedConsumerFactory,
            EmailRequestedConsumerFactory>();

        services.AddHostedService<EmailRequestedConsumer>();

        return services;
    }
}
