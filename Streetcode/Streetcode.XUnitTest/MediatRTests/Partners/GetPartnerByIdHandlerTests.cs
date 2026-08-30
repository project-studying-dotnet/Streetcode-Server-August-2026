using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.GetById;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.GetById;

public class GetPartnerByIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPartnerExists()
    {
        int partnerId = 1;
        var partner = new Partner { Id = partnerId };
        var partnerDto = new PartnerDTO { Id = partnerId };

        _repositoryMock.Setup(r => r.PartnersRepository.GetSingleOrDefaultAsync(
                It.IsAny<Expression<Func<Partner, bool>>>(),
                It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync(partner);
        _mapperMock.Setup(m => m.Map<PartnerDTO>(partner)).Returns(partnerDto);

        var handler = new GetPartnerByIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetPartnerByIdQuery(partnerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(partnerId, result.Value.Id);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenPartnerNotFound()
    {
        int partnerId = 99;
        var query = new GetPartnerByIdQuery(partnerId);
        var expectedError = $"Cannot find any partner with corresponding id: {partnerId}";

        _repositoryMock.Setup(r => r.PartnersRepository.GetSingleOrDefaultAsync(
                It.IsAny<Expression<Func<Partner, bool>>>(),
                It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync((Partner)null!);

        var handler = new GetPartnerByIdHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}