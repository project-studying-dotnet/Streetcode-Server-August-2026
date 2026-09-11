    using FluentResults;
using Moq;
using Streetcode.BLL.Interfaces.Text;
using Streetcode.BLL.MediatR.Streetcode.Text.GetParsed;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Text.GetParsed;

public class GetParsedTextAdminPreviewHandlerTests
{
    private readonly Mock<ITextService> _textServiceMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenTextIsParsed()
    {
        var text = "Test text";
        var parsedText = "<p>Test text</p>";

        _textServiceMock
            .Setup(x => x.AddTermsTag(text))
            .ReturnsAsync(parsedText);

        var handler = new GetParsedTextAdminPreviewHandler(
            _textServiceMock.Object);

        var command = new GetParsedTextForAdminPreviewCommand(text);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(parsedText, result.Value);

        _textServiceMock.Verify(
            x => x.AddTermsTag(text),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTextIsNotParsed()
    {
        var text = "Test text";

        _textServiceMock
            .Setup(x => x.AddTermsTag(text))
            .ReturnsAsync((string?)null);

        var handler = new GetParsedTextAdminPreviewHandler(
            _textServiceMock.Object);

        var command = new GetParsedTextForAdminPreviewCommand(text);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(
            "text was not parsed successfully",
            result.Errors.First().Message);

        _textServiceMock.Verify(
            x => x.AddTermsTag(text),
            Times.Once);
    }
}
