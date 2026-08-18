using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.GetById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using FactEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class GetFactByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public GetFactByIdHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactExists_ShouldReturnMappedFact()
    {
        var query = new GetFactByIdQuery(15);
        var fact = new FactEntity { Id = query.Id };
        var factDto = new FactDto { Id = query.Id };

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync(fact);
        _mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(factDto);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(factDto, result.Value);

        _factRepositoryMock.Verify(repository => repository.GetFirstOrDefaultAsync(
            It.Is<Expression<Func<FactEntity, bool>>>(predicate =>
                predicate.Compile()(new FactEntity { Id = query.Id }) &&
                !predicate.Compile()(new FactEntity { Id = query.Id + 1 })),
            It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                include => include != null)), Times.Once());
        _loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailureAndLogError()
    {
        var query = new GetFactByIdQuery(15);
        const string expectedMessage = "Cannot find any fact with corresponding id: 15";

        _factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
            .ReturnsAsync((FactEntity?)null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(query, expectedMessage),
            Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
    }

    private GetFactByIdHandler CreateHandler() =>
        new(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);
}
