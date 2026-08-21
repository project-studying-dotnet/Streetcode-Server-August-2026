using System.Linq.Expressions;
using MediatR;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class ReorderFactsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public ReorderFactsHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenOrderContainsDuplicateIds_ShouldReturnFailure()
    {
        var command = CreateCommand(10, 1, 2, 2);
        const string expectedMessage = "Fact order contains duplicate ids";

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _factRepositoryMock.VerifyNoOtherCalls();
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
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

        SetupStoredFacts(facts);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _factRepositoryMock.Verify(
            repository => repository.UpdateRange(It.IsAny<IEnumerable<FactEntity>>()),
            Times.Never());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = CreateCommand(10, 3, 1, 2);
        var facts = CreateStoredFacts();
        const string expectedMessage =
            "Failed to reorder facts for streetcode with id: 10";

        SetupStoredFacts(facts);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        AssertDisplayOrders(facts);

        _factRepositoryMock.Verify(repository => repository.UpdateRange(facts), Times.Once());
        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldUpdateDisplayOrders()
    {
        var command = CreateCommand(10, 3, 1, 2);
        var facts = CreateStoredFacts();

        SetupStoredFacts(facts);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        AssertDisplayOrders(facts);

        _factRepositoryMock.Verify(repository => repository.GetAllAsync(
            It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                predicate.Compile()(new FactEntity { StreetcodeId = 10 }) &&
                !predicate.Compile()(new FactEntity { StreetcodeId = 11 })),
            null), Times.Once());
        _factRepositoryMock.Verify(repository => repository.UpdateRange(facts), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenNoFactsAndOrderIsEmpty_ShouldReturnSuccess()
    {
        var command = CreateCommand(10);
        SetupStoredFacts(Array.Empty<FactEntity>());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);

        _factRepositoryMock.Verify(
            repo => repo.GetAllAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { StreetcodeId = 10 }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = 11 })),
                null),
            Times.Once());

        _factRepositoryMock.Verify(
            repo => repo.UpdateRange(It.IsAny<IEnumerable<FactEntity>>()),
            Times.Never());

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());

        _loggerServiceMock.VerifyNoOtherCalls();
    }

    private ReorderFactsHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _loggerServiceMock.Object);

    private void SetupStoredFacts(IEnumerable<FactEntity> facts)
    {
        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(facts);
    }

    private static ReorderFactsCommand CreateCommand(int streetcodeId, params int[] factIds) =>
        new(
            new FactReorderDto
            {
                StreetcodeId = streetcodeId,
                OrderedFactIds = factIds.ToList(),
            });

    private static List<FactEntity> CreateStoredFacts() =>
        new()
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
}
