using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetAll;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class GetAllFactsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    public GetAllFactsHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFactsOrderedByStreetcodeAndDisplayOrder()
    {
        var facts = new[]
        {
            new FactEntity { Id = 3, StreetcodeId = 2, DisplayOrder = 1 },
            new FactEntity { Id = 2, StreetcodeId = 1, DisplayOrder = 2 },
            new FactEntity { Id = 1, StreetcodeId = 1, DisplayOrder = 1 },
        };

        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                null,
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(facts);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<FactEntity>)source)
                .Select(fact => new FactDto { Id = fact.Id })
                .ToList());

        var handler = new GetAllFactsHandler(_repositoryWrapperMock.Object, _mapperMock.Object);
        var result = await handler.Handle(new GetAllFactsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2, 3 }, result.Value.Select(fact => fact.Id));

        _factRepositoryMock.Verify(repository => repository.GetAllAsync(
            null,
            It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                include => include != null)), Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()),
            Times.Once());
    }
}
