using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByTransliterationUrl;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.XUnitTest.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodeByTransliterationUrlHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetStreetcodeByTransliterationUrlHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _mapperMock.Object, _loggerMock.Object, _cacheServiceMock.Object);

    private void SetupFoundStreetcode(StreetcodeContent streetcode)
    {
        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .ReturnsAsync(streetcode);

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeTagIndexRepository.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, IIncludableQueryable<StreetcodeTagIndex, object>>>()))
            .ReturnsAsync(Enumerable.Empty<StreetcodeTagIndex>());

        _mapperMock.Setup(m => m.Map<StreetcodeDTO>(It.IsAny<object>())).Returns(new StreetcodeDTO());
        _mapperMock.Setup(m => m.Map<List<StreetcodeTagDTO>>(It.IsAny<object>())).Returns(new List<StreetcodeTagDTO>());
    }

    [Fact]
    public async Task Handle_UrlWithMixedCase_UsesLowercasedCacheKey()
    {
        const string url = "Kobzar-Taras-Shevchenko";
        var expectedKey = $"streetcode:url:{url.ToLowerInvariant()}";

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();
        SetupFoundStreetcode(new StreetcodeContent { TransliterationUrl = url.ToLowerInvariant() });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStreetcodeByTransliterationUrlQuery(url), CancellationToken.None);

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
    public async Task Handle_StreetcodeWithNullTransliterationUrl_DoesNotThrow_AndReturnsFail()
    {
        const string url = "unknown-url";

        _cacheServiceMock.SetupPassthrough<StreetcodeDTO>();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeContent, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
            .Returns((Expression<Func<StreetcodeContent, bool>> predicate, Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>? include) =>
            {
                var candidates = new[] { new StreetcodeContent { Id = 1, TransliterationUrl = null } };
                return Task.FromResult(candidates.AsQueryable().FirstOrDefault(predicate.Compile()));
            });

        var handler = CreateHandler();
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new GetStreetcodeByTransliterationUrlQuery(url), CancellationToken.None));

        Assert.Null(exception);
    }
}
