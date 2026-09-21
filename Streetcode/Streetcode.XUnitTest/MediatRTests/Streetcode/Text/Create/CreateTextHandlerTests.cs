using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.Constants;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Text.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Text.Create;

public class CreateTextHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<ITextRepository> _textRepositoryMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    public CreateTextHandlerTests()
    {
        _repositoryMock
            .Setup(r => r.TextRepository)
            .Returns(_textRepositoryMock.Object);

        _repositoryMock
            .Setup(r => r.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _streetcodeRepositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>()))
            .ReturnsAsync(new DAL.Entities.Streetcode.StreetcodeContent());

        _textRepositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.Streetcode.TextContent.Text, bool>>>()))
            .ReturnsAsync((DAL.Entities.Streetcode.TextContent.Text?)null);
    }

    [Fact]
    public async Task Handle_HidesDefaultAdditionalText()
    {
        var textDto = new TextCreateDTO
        {
            StreetcodeId = 1,
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = TextConstants.DefaultAdditionalText
        };

        var text = new DAL.Entities.Streetcode.TextContent.Text
        {
            StreetcodeId = 1,
            Title = textDto.Title,
            TextContent = textDto.TextContent,
            AdditionalText = textDto.AdditionalText
        };

        _mapperMock
            .Setup(m => m.Map<DAL.Entities.Streetcode.TextContent.Text>(textDto))
            .Returns(text);

        _textRepositoryMock
            .Setup(r => r.CreateAsync(text))
            .ReturnsAsync(text);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TextDTO>(text))
            .Returns(new TextDTO
            {
                Id = text.Id,
                Title = text.Title,
                TextContent = text.TextContent,
                StreetcodeId = text.StreetcodeId,
                AdditionalText = text.AdditionalText
            });

        var handler = new CreateTextHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var command = new CreateTextCommand(textDto);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(text.AdditionalText);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTextCreationFails()
    {
        var textDto = new TextCreateDTO
        {
            StreetcodeId = 1,
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = "Test additional text"
        };

        var text = new DAL.Entities.Streetcode.TextContent.Text
        {
            Id = 1,
            StreetcodeId = 1,
            Title = textDto.Title,
            TextContent = textDto.TextContent,
            AdditionalText = textDto.AdditionalText
        };

        _mapperMock
            .Setup(m => m.Map<DAL.Entities.Streetcode.TextContent.Text>(textDto))
            .Returns(text);

        _textRepositoryMock
            .Setup(r => r.CreateAsync(text))
            .ReturnsAsync(text);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0);

        var handler = new CreateTextHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var command = new CreateTextCommand(textDto);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(
            "Cannot save changes in the database after text creation.",
            result.Errors.First().Message);

        _textRepositoryMock.Verify(
            r => r.CreateAsync(text),
            Times.Once);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);

        _loggerMock.Verify(
            l => l.LogError(
                command,
                "Cannot save changes in the database after text creation."),
            Times.Once);
    }

    [Fact]
    public async Task Handle_KeepsAdditionalText_WhenItIsNotDefault()
    {
        var textDto = new TextCreateDTO
        {
            StreetcodeId = 1,
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = "Test additional text"
        };

        var text = new DAL.Entities.Streetcode.TextContent.Text
        {
            StreetcodeId = 1,
            Title = textDto.Title,
            TextContent = textDto.TextContent,
            AdditionalText = textDto.AdditionalText
        };

        _mapperMock
            .Setup(m => m.Map<DAL.Entities.Streetcode.TextContent.Text>(textDto))
            .Returns(text);

        _textRepositoryMock
            .Setup(r => r.CreateAsync(text))
            .ReturnsAsync(text);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TextDTO>(text))
            .Returns(new TextDTO
            {
                Id = text.Id,
                Title = text.Title,
                TextContent = text.TextContent,
                StreetcodeId = text.StreetcodeId,
                AdditionalText = text.AdditionalText
            });

        var handler = new CreateTextHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        var command = new CreateTextCommand(textDto);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Test additional text",
            text.AdditionalText);
    }
}
