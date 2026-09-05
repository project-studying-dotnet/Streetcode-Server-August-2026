using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Streetcode.BLL.Interfaces.CacheService;

namespace Streetcode.BLL.Services.CacheService
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly TimeSpan _defaultExpiration;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _defaultExpiration = TimeSpan.FromMinutes(30);
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
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to write to cache for key: {Key}", key);
            }

            return value;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
