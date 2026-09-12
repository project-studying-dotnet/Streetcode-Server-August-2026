using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Streetcode.BLL.Interfaces.CacheService
{
    public interface ICacheService
    {
        Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? expirationTime = null,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    }
}