using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using RelatedTermEntity = Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.RelatedTerm;

public class CreateRelatedTermHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IRelatedTermRepository> _relatedTermRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly CreateRelatedTermHandler _handler;

    public CreateRelatedTermHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.RelatedTermRepository)
            .Returns(_relatedTermRepositoryMock.Object);

        _handler = new CreateRelatedTermHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenInputMappingFails_ShouldReturnFailure()
    {
        var command = new CreateRelatedTermCommand(CreateRelatedTermDto());
        const string expectedError = "Cannot create new related word for a term!";

        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermEntity>(command.RelatedTerm))
            .Returns((RelatedTermEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Never());
        _relatedTermRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenExistingTermsCannotBeLoaded_ShouldReturnFailure()
    {
        var command = new CreateRelatedTermCommand(CreateRelatedTermDto());
        var relatedTerm = CreateRelatedTermEntity();
        const string expectedError = "Слово з цим визначенням уже існує";

        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermEntity>(command.RelatedTerm))
            .Returns(relatedTerm);
        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync((IEnumerable<RelatedTermEntity>)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenRelatedTermAlreadyExists_ShouldReturnFailure()
    {
        var command = new CreateRelatedTermCommand(CreateRelatedTermDto());
        var relatedTerm = CreateRelatedTermEntity();
        var existingTerms = new List<RelatedTermEntity> { relatedTerm };
        const string expectedError = "Слово з цим визначенням уже існує";

        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermEntity>(command.RelatedTerm))
            .Returns(relatedTerm);
        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync(existingTerms);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var command = new CreateRelatedTermCommand(CreateRelatedTermDto());
        var relatedTerm = CreateRelatedTermEntity();
        const string expectedError = "Cannot save changes in the database after related word creation!";

        SetupCreation(command, relatedTerm, saveChangesResult: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _relatedTermRepositoryMock.Verify(
            repository => repository.Create(relatedTerm),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<RelatedTermDTO>(It.IsAny<RelatedTermEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOutputMappingFails_ShouldReturnFailure()
    {
        var command = new CreateRelatedTermCommand(CreateRelatedTermDto());
        var relatedTerm = CreateRelatedTermEntity();
        const string expectedError = "Cannot map entity!";

        SetupCreation(command, relatedTerm, saveChangesResult: 1);
        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermDTO>(relatedTerm))
            .Returns((RelatedTermDTO)null!);

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
    public async Task Handle_WhenCreationSucceeds_ShouldReturnCreatedRelatedTerm()
    {
        var inputDto = CreateRelatedTermDto();
        var command = new CreateRelatedTermCommand(inputDto);
        var relatedTerm = CreateRelatedTermEntity();
        var expectedDto = new RelatedTermDTO
        {
            Id = 1,
            Word = inputDto.Word,
            TermId = inputDto.TermId,
        };

        SetupCreation(command, relatedTerm, saveChangesResult: 1);
        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermDTO>(relatedTerm))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _relatedTermRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.Is<Expression<Func<RelatedTermEntity, bool>>>(predicate =>
                    predicate.Compile()(relatedTerm) &&
                    !predicate.Compile()(new RelatedTermEntity
                    {
                        Word = relatedTerm.Word,
                        TermId = relatedTerm.TermId + 1,
                    })),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()),
            Times.Once());
        _relatedTermRepositoryMock.Verify(
            repository => repository.Create(relatedTerm),
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

    private void SetupCreation(
        CreateRelatedTermCommand command,
        RelatedTermEntity relatedTerm,
        int saveChangesResult)
    {
        _mapperMock
            .Setup(mapper => mapper.Map<RelatedTermEntity>(command.RelatedTerm))
            .Returns(relatedTerm);
        _relatedTermRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<RelatedTermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<RelatedTermEntity>,
                    IIncludableQueryable<RelatedTermEntity, object>>?>()))
            .ReturnsAsync(Array.Empty<RelatedTermEntity>());
        _relatedTermRepositoryMock
            .Setup(repository => repository.Create(relatedTerm))
            .Returns(relatedTerm);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(saveChangesResult);
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

    private static RelatedTermEntity CreateRelatedTermEntity()
    {
        return new RelatedTermEntity
        {
            Id = 1,
            Word = "Test",
            TermId = 5,
        };
    }
}
