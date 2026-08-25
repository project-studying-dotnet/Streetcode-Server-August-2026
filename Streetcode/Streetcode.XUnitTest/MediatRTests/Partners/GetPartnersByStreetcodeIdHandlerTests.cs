using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.GetByStreetcodeId;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.GetByStreetcodeId;

public class GetPartnersByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenStreetcodeAndPartnersExist()
    {
        int streetcodeId = 1;
        var streetcode = new DAL.Entities.Streetcode.StreetcodeContent { Id = streetcodeId };
        var partners = new List<Partner> { new() { Id = 1 } };
        var partnersDto = new List<PartnerDTO> { new() { Id = 1 } };

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetSingleOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(streetcode);
        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(It.IsAny<Expression<Func<Partner, bool>>>(), It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync(partners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerDTO>>(partners)).Returns(partnersDto);

        var handler = new GetPartnersByStreetcodeIdHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetPartnersByStreetcodeIdQuery(streetcodeId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenStreetcodeNotFound()
    {
        int streetcodeId = 99;
        var query = new GetPartnersByStreetcodeIdQuery(streetcodeId);
        var expectedError = string.Format(TestMessages.CannotFindAnyPartnersWithCorrespondingStreetcodeId, streetcodeId);

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetSingleOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync((DAL.Entities.Streetcode.StreetcodeContent)null!);

        var handler = new GetPartnersByStreetcodeIdHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        _repositoryMock.Verify(r => r.PartnersRepository.GetAllAsync(It.IsAny<Expression<Func<Partner, bool>>>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenPartnersAreNull()
    {
        int streetcodeId = 1;
        var streetcode = new DAL.Entities.Streetcode.StreetcodeContent { Id = streetcodeId };
        var query = new GetPartnersByStreetcodeIdQuery(streetcodeId);
        var expectedError = string.Format(TestMessages.CannotFindPartnersByStreetcodeId, streetcodeId);

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetSingleOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(streetcode);
        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(It.IsAny<Expression<Func<Partner, bool>>>(), It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync((IEnumerable<Partner>)null!);

        var handler = new GetPartnersByStreetcodeIdHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WithEmptyList_WhenNoPartnersMatch_EdgeCase()
    {
        int streetcodeId = 1;
        var streetcode = new DAL.Entities.Streetcode.StreetcodeContent { Id = streetcodeId };
        var emptyPartners = new List<Partner>();
        var emptyPartnersDto = new List<PartnerDTO>();

        _repositoryMock.Setup(r => r.StreetcodeRepository.GetSingleOrDefaultAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(streetcode);
        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(It.IsAny<Expression<Func<Partner, bool>>>(), It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync(emptyPartners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerDTO>>(emptyPartners)).Returns(emptyPartnersDto);

        var handler = new GetPartnersByStreetcodeIdHandler(_mapperMock.Object, _repositoryMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetPartnersByStreetcodeIdQuery(streetcodeId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}