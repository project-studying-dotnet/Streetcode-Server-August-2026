using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.GetAllPartnerShort;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.GetAllPartnerShort;

public class GetAllPartnerShortHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPartnersExist()
    {
        var partners = new List<Partner> { new() { Id = 1 } };
        var partnersDto = new List<PartnerShortDTO> { new() { Id = 1 } };

        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, null)).ReturnsAsync(partners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerShortDTO>>(partners)).Returns(partnersDto);

        var handler = new GetAllPartnerShortHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllPartnersShortQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WithEmptyList_WhenNoPartnersExist_EdgeCase()
    {
        var emptyPartners = new List<Partner>();
        var emptyPartnersDto = new List<PartnerShortDTO>();

        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, null)).ReturnsAsync(emptyPartners);
        _mapperMock.Setup(m => m.Map<IEnumerable<PartnerShortDTO>>(emptyPartners)).Returns(emptyPartnersDto);

        var handler = new GetAllPartnerShortHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetAllPartnersShortQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenPartnersAreNull()
    {
        _repositoryMock.Setup(r => r.PartnersRepository.GetAllAsync(null, null)).ReturnsAsync((IEnumerable<Partner>)null!);

        var handler = new GetAllPartnerShortHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
        var query = new GetAllPartnersShortQuery();
        var expectedError = "Cannot find any partners";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}