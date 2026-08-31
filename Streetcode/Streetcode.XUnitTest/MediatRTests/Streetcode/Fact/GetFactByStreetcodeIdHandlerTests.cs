// <copyright file="GetFactByStreetcodeIdHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.GetByStreetcodeId;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

    public class GetFactByStreetcodeIdHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<IMapper> mapperMock = new ();

    public GetFactByStreetcodeIdHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoFactsExist_ShouldReturnEmptySuccessResult()
    {
        var query = new GetFactByStreetcodeIdQuery(10);

        this.factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(Array.Empty<FactEntity>());

        this.mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
            .Returns(Array.Empty<FactDto>());

        var handler = new GetFactByStreetcodeIdHandler(
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        this.mapperMock.Verify(
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

        this.factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(facts);
        this.mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<FactEntity>)source)
                .Select(fact => new FactDto { Id = fact.Id })
                .ToList());

        var handler = new GetFactByStreetcodeIdHandler(
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2 }, result.Value.Select(fact => fact.Id));

        this.factRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { StreetcodeId = query.StreetcodeId }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = query.StreetcodeId + 1 })),
                It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                    include => include != null)),
            Times.Once());
    }
    }
}
