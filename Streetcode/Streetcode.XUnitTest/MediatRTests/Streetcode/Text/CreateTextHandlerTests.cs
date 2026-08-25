using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Text.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Text;

public class CreateTextHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<ITextRepository> _textRepositoryMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly CreateTextHandler _handler;

    public CreateTextHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.TextRepository)
            .Returns(_textRepositoryMock.Object);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _handler = new CreateTextHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
    {
        var command = new CreateTextCommand(CreateTextCreateDto());
        const string expectedError = "Cannot create text: streetcode with the given id does not exist!";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, IIncludableQueryable<StreetcodeEntity, object>>?>()))
            .ReturnsAsync((StreetcodeEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _mapperMock.Verify(
            mapper => mapper.Map<TextEntity>(It.IsAny<TextCreateDTO>()),
            Times.Never());
        _textRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<TextEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenInputMappingFails_ShouldReturnFailure()
    {
        var command = new CreateTextCommand(CreateTextCreateDto());
        const string expectedError = "Cannot create new text!";

        SetupStreetcodeExists(command);
        _mapperMock
            .Setup(mapper => mapper.Map<TextEntity>(command.TextCreateDto))
            .Returns((TextEntity)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _textRepositoryMock.Verify(
            repository => repository.Create(It.IsAny<TextEntity>()),
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
        var command = new CreateTextCommand(CreateTextCreateDto());
        var textEntity = CreateTextEntity();
        const string expectedError = "Cannot save changes in the database after text creation!";

        SetupCreation(command, textEntity, saveChangesResult: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _textRepositoryMock.Verify(
            repository => repository.Create(textEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<TextDTO>(It.IsAny<TextEntity>()),
            Times.Never());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOutputMappingFails_ShouldReturnFailure()
    {
        var command = new CreateTextCommand(CreateTextCreateDto());
        var textEntity = CreateTextEntity();
        const string expectedError = "Cannot map entity!";

        SetupCreation(command, textEntity, saveChangesResult: 1);
        _mapperMock
            .Setup(mapper => mapper.Map<TextDTO>(textEntity))
            .Returns((TextDTO)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _mapperMock.Verify(
            mapper => mapper.Map<TextDTO>(textEntity),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedError),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenCreationSucceeds_ShouldReturnCreatedText()
    {
        var inputDto = CreateTextCreateDto();
        var command = new CreateTextCommand(inputDto);
        var textEntity = CreateTextEntity();
        var expectedDto = new TextDTO
        {
            Id = 1,
            Title = inputDto.Title,
            TextContent = inputDto.TextContent,
            AdditionalText = inputDto.AdditionalText,
        };

        SetupCreation(command, textEntity, saveChangesResult: 1);
        _mapperMock
            .Setup(mapper => mapper.Map<TextDTO>(textEntity))
            .Returns(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        _streetcodeRepositoryMock.Verify(
            repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, IIncludableQueryable<StreetcodeEntity, object>>?>()),
            Times.Once());
        _textRepositoryMock.Verify(
            repository => repository.Create(textEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<TextDTO>(textEntity),
            Times.Once());
        _loggerMock.Verify(
            logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()),
            Times.Never());
    }

    private void SetupStreetcodeExists(CreateTextCommand command)
    {
        var streetcode = new StreetcodeEntity { Id = command.TextCreateDto.StreetcodeId };

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, IIncludableQueryable<StreetcodeEntity, object>>?>()))
            .ReturnsAsync(streetcode);
    }

    private void SetupCreation(
        CreateTextCommand command,
        TextEntity textEntity,
        int saveChangesResult)
    {
        SetupStreetcodeExists(command);
        _mapperMock
            .Setup(mapper => mapper.Map<TextEntity>(command.TextCreateDto))
            .Returns(textEntity);
        _textRepositoryMock
            .Setup(repository => repository.Create(textEntity))
            .Returns(textEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(saveChangesResult);
    }

    private static TextCreateDTO CreateTextCreateDto()
    {
        return new TextCreateDTO
        {
            StreetcodeId = 5,
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = "Test additional",
        };
    }

    private static TextEntity CreateTextEntity()
    {
        return new TextEntity
        {
            Id = 1,
            StreetcodeId = 5,
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = "Test additional",
        };
    }
}