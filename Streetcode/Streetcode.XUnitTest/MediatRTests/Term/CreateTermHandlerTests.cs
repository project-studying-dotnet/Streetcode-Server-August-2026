using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent;
using Streetcode.BLL.Interfaces.Logging;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Streetcode.BLL.MediatR.Streetcode.Term.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using TermEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Term;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Term;

public class CreateTermHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<ITermRepository> _termRepositoryMock;
    private readonly CreateTermHandler _handler;

    public CreateTermHandlerTests()
    {
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILoggerService>();
        _termRepositoryMock = new Mock<ITermRepository>();
        _repositoryWrapperMock
            .Setup(x => x.TermRepository)
            .Returns(_termRepositoryMock.Object);

        _handler = new CreateTermHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTermIsValid_ShouldReturnSuccess()
    {
        var termCreateDto = new TermCreateDTO
        {
            Title = " Test term ",
            Description = "Test description",
        };
        var command = new CreateTermCommand(termCreateDto);
        var expectedTermDto = new TermDTO
        {
            Id = 1,
            Title = "Test term",
            Description = "Test description",
        };
        var termEntity = new TermEntity
        {
            Title = " Test term ",
            Description = "Test description",
        };
        _termRepositoryMock
            .Setup(repo => repo.Create(termEntity))
            .Returns(termEntity);

        _mapperMock
            .Setup(mapper => mapper.Map<TermEntity>(termCreateDto))
            .Returns(termEntity);

        _termRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<TermEntity>,
                    IIncludableQueryable<TermEntity, object>>?>()))
            .ReturnsAsync((TermEntity?)null);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<TermDTO>(termEntity))
            .Returns(expectedTermDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedTermDto.Id, result.Value.Id);
        Assert.Equal(expectedTermDto.Title, result.Value.Title);
        Assert.Equal(expectedTermDto.Description, result.Value.Description);
        Assert.Equal("Test term", termEntity.Title);

        _termRepositoryMock.Verify(
            repo => repo.Create(termEntity),
            Times.Once());

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());

        _loggerMock.Verify(
            logger => logger.LogError(
                It.IsAny<object>(),
                It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenTermAlreadyExists_ShouldReturnFailure()
    {
        var termCreateDto = new TermCreateDTO
        {
            Title = "Test term",
            Description = "Test description",
        };
        var command = new CreateTermCommand(termCreateDto);
        var termEntity = new TermEntity
        {
            Title = "Test term",
            Description = "Test description",
        };
        var existingTerm = new TermEntity
        {
            Id = 1,
            Title = "Test term",
            Description = "Existing description",
        };
        const string expectedError =
            "A term with the title 'Test term' already exists.";
        _mapperMock
            .Setup(mapper => mapper.Map<TermEntity>(termCreateDto))
            .Returns(termEntity);
        _termRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<TermEntity>,
                    IIncludableQueryable<TermEntity, object>>?>()))
            .ReturnsAsync(existingTerm);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _loggerMock
            .Verify(logger => logger.LogError(command, expectedError),
                Times.Once());

        _termRepositoryMock.Verify(repo => repo.Create(
            It.IsAny<TermEntity>()),
            Times.Never());

        _repositoryWrapperMock
            .Verify(wrapper => wrapper.SaveChangesAsync(),
                Times.Never());

        _mapperMock.Verify(
            mapper => mapper.Map<TermDTO>(
                It.IsAny<TermEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
    {
        var termCreateDto = new TermCreateDTO
        {
            Title = "Test term",
            Description = "Test description",
        };

        var command = new CreateTermCommand(termCreateDto);
        var termEntity = new TermEntity
        {
            Title = "Test term",
            Description = "Test description",
        };
        const string expectedError =
            "Cannot save changes in the database after creation";

        _mapperMock
            .Setup(mapper => mapper.Map<TermEntity>(termCreateDto))
            .Returns(termEntity);
        _termRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<TermEntity>,
                    IIncludableQueryable<TermEntity, object>>?>()))
            .ReturnsAsync((TermEntity?)null);
        _termRepositoryMock
            .Setup(repo => repo.Create(termEntity))
            .Returns(termEntity);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _termRepositoryMock.Verify(
            repository => repository.Create(termEntity),
            Times.Once());

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());

        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());

        _mapperMock.Verify(
            mapper => mapper.Map<TermDTO>(It.IsAny<TermEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenMappingToEntityFails_ShouldReturnFailure()
    {
        var termCreateDto = new TermCreateDTO
        {
            Title = "Test term",
            Description = "Test description",
        };
        var command = new CreateTermCommand(termCreateDto);
        const string expectedError = "Term could not be created.";

        _mapperMock
            .Setup(mapper => mapper.Map<TermEntity>(termCreateDto))
            .Returns((TermEntity?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());

        _termRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<TermEntity>,
                    IIncludableQueryable<TermEntity, object>>?>()),
            Times.Never());

        _termRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<TermEntity>()),
            Times.Never());

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenMappingToDtoFails_ShouldReturnFailure()
    {
        var termCreateDto = new TermCreateDTO
        {
            Title = "Test term",
            Description = "Test description",
        };
        var command = new CreateTermCommand(termCreateDto);
        var termEntity = new TermEntity
        {
            Title = "Test term",
            Description = "Test description",
        };
        const string expectedError = "Cannot create term";

        _mapperMock
            .Setup(mapper => mapper.Map<TermEntity>(termCreateDto))
            .Returns(termEntity);
        _termRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TermEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<TermEntity>,
                    IIncludableQueryable<TermEntity, object>>?>()))
            .ReturnsAsync((TermEntity?)null);
        _termRepositoryMock
            .Setup(repository => repository.Create(termEntity))
            .Returns(termEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<TermDTO>(termEntity))
            .Returns((TermDTO?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);

        _termRepositoryMock.Verify(
            repository => repository.Create(termEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<TermDTO>(termEntity),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }
}
