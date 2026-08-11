using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.GetAllByTermId;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using RelatedTermEntity = Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.RelatedTerm;

public class GetAllRelatedTermsByTermIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IRelatedTermRepository> _relatedTermRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly GetAllRelatedTermsByTermIdHandler _handler;

    public GetAllRelatedTermsByTermIdHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.RelatedTermRepository)
            .Returns(_relatedTermRepositoryMock.Object);

        _handler = new GetAllRelatedTermsByTermIdHandler(
            _mapperMock.Object,
            _repositoryWrapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRelatedTermsDoNotExist_ShouldReturnFailure()
    {
        var query = new GetAllRelatedTermsByTermIdQuery(5);
        const string expectedError = "Cannot get words by term id";

        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync((IEnumerable<RelatedTermEntity>)null!);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<RelatedTermDTO>>(
                It.IsAny<IEnumerable<RelatedTermEntity>>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(query, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenMappingFails_ShouldReturnFailure()
    {
        var query = new GetAllRelatedTermsByTermIdQuery(5);
        const string expectedError = "Cannot create DTOs for related words!";
        var relatedTerms = new List<RelatedTermEntity>
        {
            new()
            {
                Id = 1,
                Word = "Test",
                TermId = query.id,
            },
        };

        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync(relatedTerms);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<RelatedTermDTO>>(relatedTerms))
            .Returns((IEnumerable<RelatedTermDTO>)null!);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<RelatedTermDTO>>(relatedTerms),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(query, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenRelatedTermsExist_ShouldReturnSuccess()
    {
        var query = new GetAllRelatedTermsByTermIdQuery(5);
        var relatedTerms = new List<RelatedTermEntity>
        {
            new() { Id = 1, Word = "First", TermId = query.id },
            new() { Id = 2, Word = "Second", TermId = query.id },
        };
        var expectedDtos = new List<RelatedTermDTO>
        {
            new() { Id = 1, Word = "First", TermId = query.id },
            new() { Id = 2, Word = "Second", TermId = query.id },
        };

        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync(relatedTerms);
        _mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<RelatedTermDTO>>(relatedTerms))
            .Returns(expectedDtos);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDtos, result.Value);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.Is<Expression<Func<RelatedTermEntity, bool>>>(predicate =>
                    predicate.Compile()(relatedTerms[0]) &&
                    !predicate.Compile()(new RelatedTermEntity { TermId = query.id + 1 })),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<RelatedTermDTO>>(relatedTerms),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }
}
