// <copyright file="ReorderFactsHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using MediatR;
    using Moq;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

    public class ReorderFactsHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<ILoggerService> loggerServiceMock = new ();

    public ReorderFactsHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenOrderContainsDuplicateIds_ShouldReturnFailure()
    {
        var command = CreateCommand(10, 1, 2, 2);
        const string expectedMessage = "Fact order contains duplicate ids";

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.factRepositoryMock.VerifyNoOtherCalls();
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenProvidedIdsDoNotMatchStoredFacts_ShouldReturnFailure()
    {
        var command = CreateCommand(10, 1, 3);
        var facts = new[]
        {
            new FactEntity { Id = 1, StreetcodeId = 10 },
            new FactEntity { Id = 2, StreetcodeId = 10 },
        };
        const string expectedMessage =
            "Provided fact ids do not match facts of streetcode with id: 10";

        this.SetupStoredFacts(facts);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.factRepositoryMock.Verify(
            repository => repository.UpdateRange(It.IsAny<IEnumerable<FactEntity>>()),
            Times.Never());
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = CreateCommand(10, 3, 1, 2);
        var facts = CreateStoredFacts();
        const string expectedMessage =
            "Failed to reorder facts for streetcode with id: 10";

        this.SetupStoredFacts(facts);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        AssertDisplayOrders(facts);

        this.factRepositoryMock.Verify(repository => repository.UpdateRange(facts), Times.Once());
        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldUpdateDisplayOrders()
    {
        var command = CreateCommand(10, 3, 1, 2);
        var facts = CreateStoredFacts();

        this.SetupStoredFacts(facts);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        AssertDisplayOrders(facts);

        this.factRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { StreetcodeId = 10 }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = 11 })),
                null),
            Times.Once());
        this.factRepositoryMock.Verify(repository => repository.UpdateRange(facts), Times.Once());
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenNoFactsAndOrderIsEmpty_ShouldReturnSuccess()
    {
        var command = CreateCommand(10);
        this.SetupStoredFacts(Array.Empty<FactEntity>());

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);

        this.factRepositoryMock.Verify(
            repo => repo.GetAllAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { StreetcodeId = 10 }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = 11 })),
                null),
            Times.Once());

        this.factRepositoryMock.Verify(
            repo => repo.UpdateRange(It.IsAny<IEnumerable<FactEntity>>()),
            Times.Never());

        this.repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());

        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    private static ReorderFactsCommand CreateCommand(int streetcodeId, params int[] factIds) =>
        new (
            new FactReorderDto
            {
                StreetcodeId = streetcodeId,
                OrderedFactIds = factIds.ToList(),
            });

    private static List<FactEntity> CreateStoredFacts() =>
        new ()
        {
            new FactEntity { Id = 1, StreetcodeId = 10, DisplayOrder = 1 },
            new FactEntity { Id = 2, StreetcodeId = 10, DisplayOrder = 2 },
            new FactEntity { Id = 3, StreetcodeId = 10, DisplayOrder = 3 },
        };

    private static void AssertDisplayOrders(IEnumerable<FactEntity> facts)
    {
        var displayOrdersById = facts.ToDictionary(fact => fact.Id, fact => fact.DisplayOrder);

        Assert.Equal(2, displayOrdersById[1]);
        Assert.Equal(3, displayOrdersById[2]);
        Assert.Equal(1, displayOrdersById[3]);
    }

    private ReorderFactsHandler CreateHandler() =>
        new (this.repositoryWrapperMock.Object, this.loggerServiceMock.Object);

    private void SetupStoredFacts(IEnumerable<FactEntity> facts)
    {
        this.factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(facts);
    }
    }
}
