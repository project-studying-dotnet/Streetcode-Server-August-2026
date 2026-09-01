// <copyright file="DeleteFactHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.Delete;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using MediatR;
    using Moq;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

    public class DeleteFactHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<ILoggerService> loggerServiceMock = new ();

    public DeleteFactHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailure()
    {
        var command = new DeleteFactCommand(15);
        const string expectedMessage = "Cannot find fact with id: 15";

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync((FactEntity?)null);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.factRepositoryMock.Verify(repository => repository.Delete(It.IsAny<FactEntity>()), Times.Never());
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
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

        this.SetupExistingFact(fact, Array.Empty<FactEntity>());
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.factRepositoryMock.Verify(
            repository => repository.UpdateRange(
                It.Is<IEnumerable<FactEntity>>(facts => !facts.Any())),
            Times.Once());
        this.factRepositoryMock.Verify(repository => repository.Delete(fact), Times.Once());
        this.loggerServiceMock.Verify(
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
            new () { Id = 16, StreetcodeId = 10, DisplayOrder = 3 },
            new () { Id = 17, StreetcodeId = 10, DisplayOrder = 4 },
        };

        this.SetupExistingFact(fact, followingFacts);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        Assert.Equal(new[] { 2, 3 }, followingFacts.Select(item => item.DisplayOrder));

        this.factRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                    predicate.Compile()(new FactEntity { StreetcodeId = 10, DisplayOrder = 3 }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = 10, DisplayOrder = 2 }) &&
                    !predicate.Compile()(new FactEntity { StreetcodeId = 20, DisplayOrder = 3 })),
                null),
            Times.Once());
        this.factRepositoryMock.Verify(repository => repository.UpdateRange(followingFacts), Times.Once());
        this.factRepositoryMock.Verify(repository => repository.Delete(fact), Times.Once());
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    private DeleteFactHandler CreateHandler() =>
        new (this.repositoryWrapperMock.Object, this.loggerServiceMock.Object);

    private void SetupExistingFact(FactEntity fact, IEnumerable<FactEntity> followingFacts)
    {
        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        this.factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(followingFacts);
    }
    }
}
