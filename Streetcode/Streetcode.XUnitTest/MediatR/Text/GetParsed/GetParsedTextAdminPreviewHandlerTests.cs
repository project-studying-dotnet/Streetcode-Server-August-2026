using Moq;
using Streetcode.BLL.Interfaces.Text;
using Streetcode.BLL.MediatR.Streetcode.Text.GetParsed;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.Text.GetParsed;

public class GetParsedTextAdminPreviewHandlerTests
{
    private readonly Mock<ITextService> _textService = new();

    [Fact]
    public async Task Handle_ShouldReturnParsedText_WhenParsingSucceeds()
    {
        const string source = "Term";
        const string parsed = "<a>Term</a>";
        _textService.Setup(x => x.AddTermsTag(source)).ReturnsAsync(parsed);

        var result = await CreateHandler().Handle(
            new GetParsedTextForAdminPreviewCommand(source), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(parsed, result.Value);
        _textService.Verify(x => x.AddTermsTag(source), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenParsingReturnsNull()
    {
        const string source = "Term";
        _textService.Setup(x => x.AddTermsTag(source)).ReturnsAsync((string)null!);

        var result = await CreateHandler().Handle(
            new GetParsedTextForAdminPreviewCommand(source), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal("text was not parsed successfully", result.Errors.Single().Message);
    }

    private GetParsedTextAdminPreviewHandler CreateHandler() => new(_textService.Object);
}
