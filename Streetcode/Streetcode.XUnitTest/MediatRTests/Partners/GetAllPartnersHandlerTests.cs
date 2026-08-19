using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.GetAll;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.GetAll;

public class GetAllPartnersHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPartnersExist()
    {
        var partners = new List<Partner> { new() { Id = 1 } };
        var partnersDto = new List<PartnerDTO> { new() { Id = 1 } };

        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync(partners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerDTO>>(partners)).Returns(partnersDto);

        var handler = new GetAllPartnersHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllPartnersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WithEmptyList_WhenNoPartnersExist_EdgeCase()
    {
        var emptyPartners = new List<Partner>();
        var emptyPartnersDto = new List<PartnerDTO>();

        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync(emptyPartners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerDTO>>(emptyPartners)).Returns(emptyPartnersDto);

        var handler = new GetAllPartnersHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllPartnersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenPartnersAreNull()
    {
        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<Partner>, IIncludableQueryable<Partner, object>>>()))
            .ReturnsAsync((IEnumerable<Partner>)null!);

        var handler = new GetAllPartnersHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllPartnersQuery();
        var expectedError = "Cannot find any partners";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}