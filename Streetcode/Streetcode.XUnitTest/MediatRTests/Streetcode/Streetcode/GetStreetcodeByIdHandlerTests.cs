using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetById;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.XUnitTest.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodeByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetStreetcodeByIdHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _mapperMock.Object, _loggerMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_ValidId_UsesExpectedCacheKey()
    {
        const int id = 42;
        var expectedKey = $"streetcode:id:{id}";

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(new StreetcodeContent { Id = id });

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeTagIndexRepository.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, IIncludableQueryable<StreetcodeTagIndex, object>>>()))
            .ReturnsAsync(Enumerable.Empty<StreetcodeTagIndex>());

        _mapperMock.Setup(m => m.Map<StreetcodeDTO>(It.IsAny<object>())).Returns(new StreetcodeDTO { Id = id });
        _mapperMock.Setup(m => m.Map<List<StreetcodeTagDTO>>(It.IsAny<object>())).Returns(new List<StreetcodeTagDTO>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeByIdQuery(id), CancellationToken.None);

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
    public async Task Handle_StreetcodeNotFound_ReturnsFail_AndDoesNotThrow()
    {
        const int id = 7;

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync((StreetcodeContent?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeByIdQuery(id), CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}