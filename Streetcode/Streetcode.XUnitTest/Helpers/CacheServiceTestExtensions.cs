using Moq;
using Streetcode.BLL.Interfaces.CacheService;

namespace Streetcode.XUnitTest.Helpers;

public static class CacheServiceTestExtensions
{
    public static void SetupPassthrough<T>(this Mock<ICacheService> cacheMock)
    {
        cacheMock
            .Setup(cache => cache.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<T?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<CancellationToken, Task<T?>> factory, TimeSpan? _, CancellationToken cancellationToken) =>
                factory(cancellationToken));
    }
}
