using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.BLL.Interfaces.CacheService;

namespace Streetcode.BLL.Services.CacheService;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger, IOptions<CacheOptions> cacheOptions)
    {
        _cache = cache;
        _logger = logger;
        _defaultExpiration = TimeSpan.FromMinutes(cacheOptions.Value.DefaultExpirationMinutes);
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);

            if (!string.IsNullOrWhiteSpace(cachedValue))
            {
                var deserialized = JsonSerializer.Deserialize<T>(cachedValue);

                if (deserialized is not null)
                {
                    return deserialized;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read from cache for key: {Key}", key);
        }

        var value = await factory(cancellationToken);

        if (value is null)
        {
            return default;
        }

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? _defaultExpiration
            };

            var serialized = JsonSerializer.Serialize(value);

            await _cache.SetStringAsync(key, serialized, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to cache for key: {Key}", key);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} to remove cache key {Key} failed", attempt, key);

                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Exhausted retries removing cache key {Key}", key);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt - 1)), CancellationToken.None);
                }
            }
        }
    }

    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
        await Task.WhenAll(tasks);
    }
}
