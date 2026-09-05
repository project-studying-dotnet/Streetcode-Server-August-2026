using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Text.GetById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;
using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.XUnitTest.MediatRTests.Text.GetById;

public class GetTextByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapper = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILoggerService> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnMappedText_WhenTextExists()
    {
        var text = new TextEntity { Id = 7, Title = "Title", TextContent = "Content" };
        var textDto = new TextDTO { Id = 7, Title = "Title", TextContent = "Content" };
        _repositoryWrapper.Setup(x => x.TextRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TextEntity, bool>>>(), null))
            .ReturnsAsync(text);
        _mapper.Setup(x => x.Map<TextDTO>(text)).Returns(textDto);

        var result = await CreateHandler().Handle(new GetTextByIdQuery(7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(textDto, result.Value);
    }

    [Fact]
    public async Task Handle_ShouldUseRequestedIdInRepositoryPredicate()
    {
        Expression<Func<TextEntity, bool>>? capturedPredicate = null;
        _repositoryWrapper.Setup(x => x.TextRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TextEntity, bool>>>(), null))
            .Callback<Expression<Func<TextEntity, bool>>?, Func<IQueryable<TextEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<TextEntity, object>>?>(
                (predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new TextEntity { Id = 11 });
        _mapper.Setup(x => x.Map<TextDTO>(It.IsAny<TextEntity>())).Returns(new TextDTO());

        await CreateHandler().Handle(new GetTextByIdQuery(11), CancellationToken.None);

        Assert.NotNull(capturedPredicate);
        Assert.True(capturedPredicate.Compile()(new TextEntity { Id = 11 }));
        Assert.False(capturedPredicate.Compile()(new TextEntity { Id = 12 }));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogError_WhenTextDoesNotExist()
    {
        var query = new GetTextByIdQuery(42);
        const string expectedMessage = "Cannot find any text with corresponding id: 42";
        _repositoryWrapper.Setup(x => x.TextRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TextEntity, bool>>>(), null))
            .ReturnsAsync((TextEntity?)null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        _logger.Verify(x => x.LogError(query, expectedMessage), Times.Once);
        _mapper.Verify(x => x.Map<TextDTO>(It.IsAny<TextEntity>()), Times.Never);
    }

    private GetTextByIdHandler CreateHandler() =>
        new(_repositoryWrapper.Object, _mapper.Object, _logger.Object);
}
