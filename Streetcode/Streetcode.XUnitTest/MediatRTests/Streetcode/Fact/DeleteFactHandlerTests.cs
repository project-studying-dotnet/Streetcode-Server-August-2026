using System.Linq.Expressions;
using MediatR;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class DeleteFactHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public DeleteFactHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailure()
    {
        var command = new DeleteFactCommand(15);
        const string expectedMessage = "Cannot find fact with id: 15";

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync((FactEntity?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        _factRepositoryMock.Verify(repository => repository.Delete(It.IsAny<FactEntity>()), Times.Never());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = new DeleteFactCommand(15);
        var fact = new FactEntity
        {
            Id = command.Id,
            StreetcodeId = 10,
            DisplayOrder = 2,
        };
        const string expectedMessage = "Failed to delete fact with id: 15";

        SetupExistingFact(fact, Array.Empty<FactEntity>());
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _factRepositoryMock.Verify(repository => repository.UpdateRange(
            It.Is<IEnumerable<FactEntity>>(facts => !facts.Any())), Times.Once());
        _factRepositoryMock.Verify(repository => repository.Delete(fact), Times.Once());
        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenFactExists_ShouldDeleteFactAndShiftFollowingFacts()
    {
        var command = new DeleteFactCommand(15);
        var fact = new FactEntity
        {
            Id = command.Id,
            StreetcodeId = 10,
            DisplayOrder = 2,
        };
        var followingFacts = new List<FactEntity>
        {
            new() { Id = 16, StreetcodeId = 10, DisplayOrder = 3 },
            new() { Id = 17, StreetcodeId = 10, DisplayOrder = 4 },
        };

        SetupExistingFact(fact, followingFacts);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        Assert.Equal(new[] { 2, 3 }, followingFacts.Select(item => item.DisplayOrder));

        _factRepositoryMock.Verify(repository => repository.GetAllAsync(
            It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                predicate.Compile()(new FactEntity { StreetcodeId = 10, DisplayOrder = 3 }) &&
                !predicate.Compile()(new FactEntity { StreetcodeId = 10, DisplayOrder = 2 }) &&
                !predicate.Compile()(new FactEntity { StreetcodeId = 20, DisplayOrder = 3 })),
            null), Times.Once());
        _factRepositoryMock.Verify(repository => repository.UpdateRange(followingFacts), Times.Once());
        _factRepositoryMock.Verify(repository => repository.Delete(fact), Times.Once());
        _repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        _loggerServiceMock.VerifyNoOtherCalls();
    }

    private DeleteFactHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _loggerServiceMock.Object);

    private void SetupExistingFact(FactEntity fact, IEnumerable<FactEntity> followingFacts)
    {
        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        _factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(followingFacts);
    }
}
