using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Infrastructure.Messaging.Kafka;

namespace Streetcode.Identity.Infrastructure.Persistence.Outbox;

public sealed class OutboxPublisherBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _pollingInterval;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _pollingInterval = options.Value.OutboxPollingInterval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var publisher = scope.ServiceProvider
                    .GetRequiredService<OutboxPublisher>();

                await publisher.PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error while processing the outbox");
            }

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
