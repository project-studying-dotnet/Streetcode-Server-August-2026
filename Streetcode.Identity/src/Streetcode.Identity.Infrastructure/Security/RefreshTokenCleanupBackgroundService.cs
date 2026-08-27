using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Streetcode.Identity.Infrastructure.Security;

public sealed class RefreshTokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<RefreshTokenCleanupBackgroundService> _logger;

    public RefreshTokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RefreshTokenCleanupOptions> options,
        ILogger<RefreshTokenCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = options.Value.Interval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var cleanupService = scope.ServiceProvider
                    .GetRequiredService<RefreshTokenCleanupService>();

                var deletedCount =
                    await cleanupService.CleanupAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Removed {DeletedCount} expired refresh tokens.", deletedCount);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while cleaning up refresh tokens.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
