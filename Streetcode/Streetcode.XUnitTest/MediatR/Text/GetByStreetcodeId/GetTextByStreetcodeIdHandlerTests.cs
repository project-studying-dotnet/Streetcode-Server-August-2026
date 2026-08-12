using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.Interfaces.Text;
using Streetcode.BLL.MediatR.Streetcode.Text.GetByStreetcodeId;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.XUnitTest.MediatR.Text.GetByStreetcodeId;

public class GetTextByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapper = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ITextService> _textService = new();
    private readonly Mock<ILoggerService> _logger = new();

    [Fact]
    public async Task Handle_ShouldAddTermTagsAndReturnMappedText_WhenTextExists()
    {
        var text = new TextEntity { Id = 1, StreetcodeId = 9, TextContent = "Term" };
        var dto = new TextDTO { Id = 1, StreetcodeId = 9, TextContent = "<a>Term</a>" };
        SetupText(text);
        _textService.Setup(x => x.AddTermsTag("Term")).ReturnsAsync("<a>Term</a>");
        _mapper.Setup(x => x.Map<TextDTO?>(text)).Returns(dto);

        var result = await CreateHandler().Handle(new GetTextByStreetcodeIdQuery(9), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(dto, result.Value);
        Assert.Equal("<a>Term</a>", text.TextContent);
        _textService.Verify(x => x.AddTermsTag("Term"), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessfulNullResult_WhenStreetcodeExistsWithoutText()
    {
        SetupText(null);
        _repositoryWrapper.Setup(x => x.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(new StreetcodeContent { Id = 9 });

        var result = await CreateHandler().Handle(new GetTextByStreetcodeIdQuery(9), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        _textService.Verify(x => x.AddTermsTag(It.IsAny<string>()), Times.Never);
        _mapper.Verify(x => x.Map<TextDTO?>(It.IsAny<TextEntity>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogError_WhenStreetcodeDoesNotExist()
    {
        var query = new GetTextByStreetcodeIdQuery(99);
        const string expectedMessage = "Cannot find a transaction link by a streetcode id: 99, because such streetcode doesn`t exist";
        SetupText(null);
        _repositoryWrapper.Setup(x => x.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
            .ReturnsAsync((StreetcodeContent?)null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        _logger.Verify(x => x.LogError(query, expectedMessage), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseRequestedStreetcodeIdInTextPredicate()
    {
        Expression<Func<TextEntity, bool>>? capturedPredicate = null;
        _repositoryWrapper.Setup(x => x.TextRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TextEntity, bool>>>(), null))
            .Callback<Expression<Func<TextEntity, bool>>?, Func<IQueryable<TextEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<TextEntity, object>>?>(
                (predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new TextEntity { StreetcodeId = 15, TextContent = string.Empty });
        _textService.Setup(x => x.AddTermsTag(string.Empty)).ReturnsAsync(string.Empty);
        _mapper.Setup(x => x.Map<TextDTO?>(It.IsAny<TextEntity>())).Returns(new TextDTO());

        await CreateHandler().Handle(new GetTextByStreetcodeIdQuery(15), CancellationToken.None);

        Assert.NotNull(capturedPredicate);
        Assert.True(capturedPredicate.Compile()(new TextEntity { StreetcodeId = 15 }));
        Assert.False(capturedPredicate.Compile()(new TextEntity { StreetcodeId = 16 }));
    }

    private void SetupText(TextEntity? text) =>
        _repositoryWrapper.Setup(x => x.TextRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TextEntity, bool>>>(), null))
            .ReturnsAsync(text);

    private GetTextByStreetcodeIdHandler CreateHandler() =>
        new(_repositoryWrapper.Object, _mapper.Object, _textService.Object, _logger.Object);
}
