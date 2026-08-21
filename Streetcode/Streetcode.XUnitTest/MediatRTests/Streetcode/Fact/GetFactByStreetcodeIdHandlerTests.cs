using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetByStreetcodeId;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class GetFactByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public GetFactByStreetcodeIdHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoFactsExist_ShouldReturnEmptySuccessResult()
    {
        var query = new GetFactByStreetcodeIdQuery(10);

        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(Array.Empty<FactEntity>());

        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
            .Returns(Array.Empty<FactDto>());

        var handler = new GetFactByStreetcodeIdHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _loggerServiceMock.VerifyNoOtherCalls();
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldFilterByStreetcodeAndOrderByDisplayOrder()
    {
        var query = new GetFactByStreetcodeIdQuery(10);
        var facts = new[]
        {
            new FactEntity { Id = 2, StreetcodeId = query.StreetcodeId, DisplayOrder = 2 },
            new FactEntity { Id = 1, StreetcodeId = query.StreetcodeId, DisplayOrder = 1 },
        };

        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(facts);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<FactEntity>)source)
                .Select(fact => new FactDto { Id = fact.Id })
                .ToList());

        var handler = new GetFactByStreetcodeIdHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2 }, result.Value.Select(fact => fact.Id));

        _factRepositoryMock.Verify(repository => repository.GetAllAsync(
            It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                predicate.Compile()(new FactEntity { StreetcodeId = query.StreetcodeId }) &&
                !predicate.Compile()(new FactEntity { StreetcodeId = query.StreetcodeId + 1 })),
            It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                include => include != null)), Times.Once());
    }
}
