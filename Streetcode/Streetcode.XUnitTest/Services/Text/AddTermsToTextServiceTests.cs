using System.Linq.Expressions;
using Moq;
using Streetcode.BLL.Services.Text;
using Streetcode.DAL.Entities.Streetcode.TextContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using Xunit;

namespace Streetcode.XUnitTest.Services.Text;

public class AddTermsToTextServiceTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<ITermRepository> _termRepositoryMock = new();
    private readonly Mock<IRelatedTermRepository> _relatedTermRepositoryMock = new();

    public AddTermsToTextServiceTests()
    {
        _repositoryMock
            .Setup(r => r.TermRepository)
            .Returns(_termRepositoryMock.Object);

        _repositoryMock
            .Setup(r => r.RelatedTermRepository)
            .Returns(_relatedTermRepositoryMock.Object);

        _termRepositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Term, bool>>>(),
                It.IsAny<Func<IQueryable<Term>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Term, object>>>()))
            .ReturnsAsync((Term?)null);

        _relatedTermRepositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<RelatedTerm, bool>>>(),
                It.IsAny<Func<IQueryable<RelatedTerm>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<RelatedTerm, object>>>()))
            .ReturnsAsync((RelatedTerm?)null);
    }

    [Fact]
    public async Task AddTermsTag_PreservesBoldAndItalicFormatting()
    {
        var service = new AddTermsToTextService(_repositoryMock.Object);

        var text = "Приклад <strong>жирного</strong> та <em>курсивного</em> тексту.";

        var result = await service.AddTermsTag(text);

        Assert.Contains("<strong>", result);
        Assert.Contains("</strong>", result);
        Assert.Contains("жирного", result);

        Assert.Contains("<em>", result);
        Assert.Contains("</em>", result);
        Assert.Contains("курсивного", result);
    }
}