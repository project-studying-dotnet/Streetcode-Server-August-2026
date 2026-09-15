using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetShortById;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.XUnitTest.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodeShortByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetStreetcodeShortByIdHandler CreateHandler() =>
        new(_mapperMock.Object, _repositoryWrapperMock.Object, _loggerMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_ValidId_UsesExpectedCacheKey()
    {
        const int id = 5;
        var expectedKey = $"streetcode:short:{id}";

        _cacheServiceMock.SetupPassthrough<StreetcodeShortDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(new StreetcodeContent { Id = id });

        _mapperMock.Setup(m => m.Map<StreetcodeShortDTO>(It.IsAny<object>())).Returns(new StreetcodeShortDTO { Id = id });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeShortByIdQuery(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _cacheServiceMock.Verify(
            c => c.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<StreetcodeShortDTO?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StreetcodeNotFound_ReturnsFail()
    {
        const int id = 404;

        _cacheServiceMock.SetupPassthrough<StreetcodeShortDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync((StreetcodeContent?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeShortByIdQuery(id), CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
