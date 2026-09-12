using System;
using System.Collections.Generic;
using System.Text;
using Streetcode.BLL.Interfaces.CacheService;

namespace Streetcode.BLL.Services.CacheService;

public class NoOpCacheService : ICacheService
{
    public Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        return factory(cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
