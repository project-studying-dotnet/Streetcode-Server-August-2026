using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.Services.CacheService
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _distributedCacheMock;
        private readonly Mock<ILogger<Streetcode.BLL.Services.CacheService.CacheService>> _loggerMock;
        private readonly Streetcode.BLL.Services.CacheService.CacheService _sut;

        private record TestDto(int Id, string Name);

        public CacheServiceTests()
        {
            _distributedCacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<Streetcode.BLL.Services.CacheService.CacheService>>();
            _sut = new Streetcode.BLL.Services.CacheService.CacheService(_distributedCacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetOrCreateAsync_CacheHit_ReturnsDeserializedValue_AndDoesNotCallFactory()
        {
            var dto = new TestDto(1, "Cached");
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto));

            _distributedCacheMock
                .Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            var factoryCalled = false;
            Func<CancellationToken, Task<TestDto?>> factory = _ =>
            {
                factoryCalled = true;
                return Task.FromResult<TestDto?>(new TestDto(999, "ShouldNotBeUsed"));
            };

            var result = await _sut.GetOrCreateAsync("key", factory);

            Assert.NotNull(result);
            Assert.Equal(dto.Id, result!.Id);
            Assert.Equal(dto.Name, result.Name);
            Assert.False(factoryCalled);
        }

        [Fact]
        public async Task GetOrCreateAsync_CacheMiss_CallsFactory_AndWritesToCache()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            var expected = new TestDto(1, "FromFactory");
            var factoryCalled = false;
            Func<CancellationToken, Task<TestDto?>> factory = _ =>
            {
                factoryCalled = true;
                return Task.FromResult<TestDto?>(expected);
            };

            var result = await _sut.GetOrCreateAsync("key", factory);

            Assert.True(factoryCalled);
            Assert.Equal(expected, result);

            _distributedCacheMock.Verify(
                c => c.SetAsync(
                    "key",
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetOrCreateAsync_FactoryReturnsNull_ReturnsDefault_AndDoesNotWriteToCache()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(null);

            var result = await _sut.GetOrCreateAsync("key", factory);

            Assert.Null(result);

            _distributedCacheMock.Verify(
                c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetOrCreateAsync_WritesToCache_WithProvidedExpiration()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            var expiration = TimeSpan.FromMinutes(5);
            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(new TestDto(1, "X"));

            await _sut.GetOrCreateAsync("key", factory, expiration);

            _distributedCacheMock.Verify(
                c => c.SetAsync(
                    "key",
                    It.IsAny<byte[]>(),
                    It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetOrCreateAsync_NoExpirationProvided_UsesDefaultExpiration()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(new TestDto(1, "X"));

            await _sut.GetOrCreateAsync("key", factory);

            _distributedCacheMock.Verify(
                c => c.SetAsync(
                    "key",
                    It.IsAny<byte[]>(),
                    It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(30)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetOrCreateAsync_ReadThrows_FallsBackToFactory_AndDoesNotPropagate()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Redis is down"));

            var expected = new TestDto(1, "FromFactory");
            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(expected);

            var result = await _sut.GetOrCreateAsync("key", factory);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetOrCreateAsync_WriteThrows_StillReturnsFactoryValue_AndDoesNotPropagate()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            _distributedCacheMock
                .Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Redis is down"));

            var expected = new TestDto(1, "FromFactory");
            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(expected);

            var result = await _sut.GetOrCreateAsync("key", factory);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task RemoveAsync_Throws_DoesNotPropagate()
        {
            _distributedCacheMock
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Redis is down"));

            var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync("key"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task GetOrCreateAsync_ReadCancelled_PropagatesOperationCanceledException()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(new TestDto(1, "X"));

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _sut.GetOrCreateAsync("key", factory));
        }

        [Fact]
        public async Task GetOrCreateAsync_WriteCancelled_PropagatesOperationCanceledException()
        {
            _distributedCacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            _distributedCacheMock
                .Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Func<CancellationToken, Task<TestDto?>> factory = _ => Task.FromResult<TestDto?>(new TestDto(1, "X"));

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _sut.GetOrCreateAsync("key", factory));
        }

        [Fact]
        public async Task RemoveAsync_Cancelled_PropagatesOperationCanceledException()
        {
            _distributedCacheMock
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _sut.RemoveAsync("key"));
        }

        [Fact]
        public async Task RemoveAsync_SingleKey_CallsDistributedCacheRemove()
        {
            await _sut.RemoveAsync("key");

            _distributedCacheMock.Verify(
                c => c.RemoveAsync("key", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_MultipleKeys_RemovesEachKey()
        {
            var keys = new[] { "key1", "key2", "key3" };

            await _sut.RemoveAsync(keys);

            foreach (var key in keys)
            {
                _distributedCacheMock.Verify(
                    c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task RemoveAsync_OneKeyThrows_OtherKeysStillRemoved()
        {
            _distributedCacheMock
                .Setup(c => c.RemoveAsync("bad-key", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Redis is down"));

            var keys = new[] { "bad-key", "good-key" };

            await _sut.RemoveAsync(keys);

            _distributedCacheMock.Verify(
                c => c.RemoveAsync("good-key", It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
