using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Text.GetAll;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.XUnitTest.MediatRTests.Text.GetAll;

public class GetAllTextsHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapper = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILoggerService> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnMappedTexts_WhenTextsExist()
    {
        var texts = new List<TextEntity> { new() { Id = 1, Title = "Title", TextContent = "Content" } };
        var textDtos = new List<TextDTO> { new() { Id = 1, Title = "Title", TextContent = "Content" } };
        _repositoryWrapper.Setup(x => x.TextRepository.GetAllAsync(null, null)).ReturnsAsync(texts);
        _mapper.Setup(x => x.Map<IEnumerable<TextDTO>>(texts)).Returns(textDtos);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetAllTextsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(textDtos, result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoTextsExist()
    {
        var texts = new List<TextEntity>();
        var textDtos = new List<TextDTO>();
        _repositoryWrapper.Setup(x => x.TextRepository.GetAllAsync(null, null)).ReturnsAsync(texts);
        _mapper.Setup(x => x.Map<IEnumerable<TextDTO>>(texts)).Returns(textDtos);

        var result = await CreateHandler().Handle(new GetAllTextsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogError_WhenRepositoryReturnsNull()
    {
        var query = new GetAllTextsQuery();
        const string expectedMessage = "Cannot find any text";
        _repositoryWrapper.Setup(x => x.TextRepository.GetAllAsync(null, null))
            .ReturnsAsync((IEnumerable<TextEntity>)null!);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        _logger.Verify(x => x.LogError(query, expectedMessage), Times.Once);
        _mapper.Verify(x => x.Map<IEnumerable<TextDTO>>(It.IsAny<object>()), Times.Never);
    }

    private GetAllTextsHandler CreateHandler() =>
        new(_repositoryWrapper.Object, _mapper.Object, _logger.Object);
}
