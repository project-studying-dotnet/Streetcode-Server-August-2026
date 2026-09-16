using Streetcode.BLL.Services.CacheService;
using Xunit;

namespace Streetcode.XUnitTest.Services.CacheService;

public class NoOpCacheServiceTests
{
    private readonly NoOpCacheService _sut = new ();

    [Fact]
    public async Task GetOrCreateAsync_AlwaysCallsFactory_AndReturnsFactoryValue()
    {
        var factoryCalled = false;
        Func<CancellationToken, Task<string?>> factory = _ =>
        {
            factoryCalled = true;
            return Task.FromResult<string?>("from-factory");
        };

        var result = await _sut.GetOrCreateAsync("any-key", factory, TimeSpan.FromMinutes(5));

        Assert.True(factoryCalled);
        Assert.Equal("from-factory", result);
    }

    [Fact]
    public async Task RemoveAsync_SingleKey_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync("key"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RemoveAsync_MultipleKeys_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync(new[] { "key1", "key2" }));

        Assert.Null(exception);
    }
}
