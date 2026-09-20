using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.DeleteSoft;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class DeleteSoftStreetcodeHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private DeleteSoftStreetcodeHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _loggerMock.Object, _cacheServiceMock.Object);

    private void SetupFoundStreetcode(StreetcodeContent streetcode)
    {
        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(streetcode);

        _repositoryWrapperMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_SuccessfulDelete_RemovesAllFourExpectedCacheKeys()
    {
        var streetcode = new StreetcodeContent
        {
            Id = 10,
            Index = 3,
            TransliterationUrl = "Taras-Shevchenko",
        };
        SetupFoundStreetcode(streetcode);

        var expectedKeys = new[]
        {
            "streetcode:id:10",
            "streetcode:short:10",
            "streetcode:index:3",
            "streetcode:url:taras-shevchenko",
        };

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSoftStreetcodeCommand(streetcode.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(
                It.Is<IEnumerable<string>>(keys => new HashSet<string>(keys).SetEquals(expectedKeys)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StreetcodeWithNullTransliterationUrl_UsesEmptyUrlKey_AndDoesNotThrow()
    {
        var streetcode = new StreetcodeContent
        {
            Id = 20,
            Index = 4,
            TransliterationUrl = null,
        };
        SetupFoundStreetcode(streetcode);

        var expectedKeys = new[]
        {
            "streetcode:id:20",
            "streetcode:short:20",
            "streetcode:index:4",
            "streetcode:url:",
        };

        var handler = CreateHandler();
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new DeleteSoftStreetcodeCommand(streetcode.Id), CancellationToken.None));

        Assert.Null(exception);

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(
                It.Is<IEnumerable<string>>(keys => new HashSet<string>(keys).SetEquals(expectedKeys)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StreetcodeNotFound_ThrowsArgumentNullException_AndDoesNotRemoveFromCache()
    {
        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync((StreetcodeContent?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(new DeleteSoftStreetcodeCommand(999), CancellationToken.None));

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SaveChangesFails_ReturnsFail_AndDoesNotRemoveFromCache()
    {
        var streetcode = new StreetcodeContent { Id = 30, Index = 5, TransliterationUrl = "x" };

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(streetcode);

        _repositoryWrapperMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSoftStreetcodeCommand(streetcode.Id), CancellationToken.None);

        Assert.True(result.IsFailed);

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
