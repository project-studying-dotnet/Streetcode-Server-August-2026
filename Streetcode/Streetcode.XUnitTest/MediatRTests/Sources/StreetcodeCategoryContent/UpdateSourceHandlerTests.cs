using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Update;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Source;
using Xunit;
using SourceEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

namespace Streetcode.XUnitTest.MediatRTests.Sources.StreetcodeCategoryContent;

public class UpdateSourceHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeCategoryContentRepository>
        _sourceRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    public UpdateSourceHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeCategoryContentRepository)
            .Returns(_sourceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSourceExists_ShouldUpdateSource()
    {
        var sourceDto = new SourceUpdateDTO(
            StreetcodeId: 1,
            SourceLinkCategoryId: 2,
            Text: "Updated source text");
        var command = new UpdateSourceCommand(sourceDto);
        var sourceEntity = new SourceEntity
        {
            StreetcodeId = sourceDto.StreetcodeId,
            SourceLinkCategoryId = sourceDto.SourceLinkCategoryId,
            Text = "Old source text",
        };
        var expectedDto = new StreetcodeCategoryContentDTO
        {
            StreetcodeId = sourceDto.StreetcodeId,
            SourceLinkCategoryId = sourceDto.SourceLinkCategoryId,
            Text = sourceDto.Text,
        };

        SetupSourceLookup(sourceEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock
            .Setup(mapper => mapper.Map<StreetcodeCategoryContentDTO>(
                sourceEntity))
            .Returns(expectedDto);
        var handler = CreateHandler();

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDto, result.Value);
        Assert.Equal(sourceDto.Text, sourceEntity.Text);
        _sourceRepositoryMock.Verify(
            repository => repository.Update(sourceEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.Verify(
            mapper => mapper.Map<StreetcodeCategoryContentDTO>(sourceEntity),
            Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenSourceDoesNotExist_ShouldReturnFailure()
    {
        var sourceDto = new SourceUpdateDTO(
            StreetcodeId: 1,
            SourceLinkCategoryId: 2,
            Text: "Updated source text");
        var command = new UpdateSourceCommand(sourceDto);
        const string expectedErrorMessage =
            "Cannot find source block for streetcode id: 1 and category id: 2";

        SetupSourceLookup(null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);
        _sourceRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<SourceEntity>()),
            Times.Never());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedErrorMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSaveFails_ShouldReturnFailure()
    {
        var sourceDto = new SourceUpdateDTO(
            StreetcodeId: 1,
            SourceLinkCategoryId: 2,
            Text: "Updated source text");
        var command = new UpdateSourceCommand(sourceDto);
        var sourceEntity = new SourceEntity
        {
            StreetcodeId = sourceDto.StreetcodeId,
            SourceLinkCategoryId = sourceDto.SourceLinkCategoryId,
            Text = "Old source text",
        };
        const string expectedErrorMessage =
            "Failed to update source block.";

        SetupSourceLookup(sourceEntity);
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);
        var handler = CreateHandler();

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);
        Assert.Equal(sourceDto.Text, sourceEntity.Text);
        _sourceRepositoryMock.Verify(
            repository => repository.Update(sourceEntity),
            Times.Once());
        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Once());
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.Verify(
            logger => logger.LogError(command, expectedErrorMessage),
            Times.Once());
    }

    private void SetupSourceLookup(SourceEntity? source)
    {
        _sourceRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<SourceEntity, bool>>>(),
                null))
            .ReturnsAsync(source);
    }

    private UpdateSourceHandler CreateHandler()
    {
        return new UpdateSourceHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }
}
