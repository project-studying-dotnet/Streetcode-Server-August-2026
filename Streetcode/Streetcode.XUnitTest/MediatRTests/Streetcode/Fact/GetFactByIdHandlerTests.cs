// <copyright file="GetFactByIdHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.GetById;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

    public class GetFactByIdHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<IMapper> mapperMock = new ();
    private readonly Mock<ILoggerService> loggerServiceMock = new ();

    public GetFactByIdHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactExists_ShouldReturnMappedFact()
    {
        var query = new GetFactByIdQuery(15);
        var fact = new FactEntity { Id = query.Id };
        var factDto = new FactDto { Id = query.Id };

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(fact);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(factDto);

        var result = await this.CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(factDto, result.Value);

        this.factRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { Id = query.Id }) &&
                    !predicate.Compile()(new FactEntity { Id = query.Id + 1 })),
                It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                    include => include != null)),
            Times.Once());
        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailureAndLogError()
    {
        var query = new GetFactByIdQuery(15);
        const string expectedMessage = "Cannot find any fact with corresponding id: 15";

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync((FactEntity?)null);

        var result = await this.CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(query, expectedMessage),
            Times.Once());
        this.mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
    }

    private GetFactByIdHandler CreateHandler() =>
        new (
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object,
            this.loggerServiceMock.Object);
    }
}
