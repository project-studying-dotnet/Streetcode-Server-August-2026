using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using RelatedTermEntity = Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.RelatedTerm;

public class DeleteRelatedTermHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IRelatedTermRepository> _relatedTermRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly DeleteRelatedTermHandler _handler;

    public DeleteRelatedTermHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.RelatedTermRepository)
            .Returns(_relatedTermRepositoryMock.Object);

        _handler = new DeleteRelatedTermHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRelatedTermDoesNotExist_ShouldReturnFailure()
    {
        var command = new DeleteRelatedTermCommand("Missing");
        var expectedError = string.Format(TestMessages.CannotFindRelatedTerm, command.word);

        _relatedTermRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync((RelatedTermEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _mapperMock.Verify(
            mapper => mapper.Map<RelatedTermDTO>(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = new DeleteRelatedTermCommand("Test");
        var relatedTerm = CreateRelatedTermEntity();
        var relatedTermDto = CreateRelatedTermDto();
        var expectedError = TestMessages.FailedToDeleteRelatedTerm;

        SetupDeletion(relatedTerm, relatedTermDto, saveChangesResult: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.Delete(relatedTerm),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<RelatedTermDTO>(relatedTerm),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOutputMappingFails_ShouldReturnFailure()
    {
        var command = new DeleteRelatedTermCommand("Test");
        var relatedTerm = CreateRelatedTermEntity();
        var expectedError = TestMessages.FailedToDeleteRelatedTerm;

        SetupDeletion(relatedTerm, null, saveChangesResult: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _mapperMock.Verify(
            mapper => mapper.Map<RelatedTermDTO>(relatedTerm),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDeletionSucceeds_ShouldReturnDeletedRelatedTerm()
    {
        var command = new DeleteRelatedTermCommand("tEsT");
        var relatedTerm = CreateRelatedTermEntity();
        var expectedDto = CreateRelatedTermDto();

        SetupDeletion(relatedTerm, expectedDto, saveChangesResult: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<RelatedTermEntity, bool>>>(predicate =>
                    predicate.Compile()(relatedTerm) &&
                    !predicate.Compile()(new RelatedTermEntity { Word = "Other" })),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Once());
        _relatedTermRepositoryMock.Verify(
            repository => repository.Delete(relatedTerm),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<RelatedTermDTO>(relatedTerm),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    private void SetupDeletion(
        RelatedTermEntity relatedTerm,
        RelatedTermDTO? relatedTermDto,
        int saveChangesResult)
    {
        _relatedTermRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync(relatedTerm);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(saveChangesResult);
        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermDTO>(relatedTerm))
            .Returns(relatedTermDto!);
    }

    private static RelatedTermEntity CreateRelatedTermEntity()
    {
        return new RelatedTermEntity
        {
            Id = 1,
            Word = "Test",
            TermId = 5,
        };
    }

    private static RelatedTermDTO CreateRelatedTermDto()
    {
        return new RelatedTermDTO
        {
            Id = 1,
            Word = "Test",
            TermId = 5,
        };
    }
}
