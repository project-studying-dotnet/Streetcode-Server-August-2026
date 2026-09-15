using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIndex;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.XUnitTest.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodeByIndexHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetStreetcodeByIndexHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _mapperMock.Object, _loggerMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_ValidIndex_UsesExpectedCacheKey()
    {
        const int index = 15;
        var expectedKey = $"streetcode:index:{index}";

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(new StreetcodeContent { Index = index });

        _mapperMock.Setup(m => m.Map<StreetcodeDTO>(It.IsAny<object>())).Returns(new StreetcodeDTO { Index = index });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeByIndexQuery(index), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _cacheServiceMock.Verify(
            c => c.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<StreetcodeDTO?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StreetcodeNotFound_ReturnsFail()
    {
        const int index = 99;

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync((StreetcodeContent?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeByIndexQuery(index), CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}