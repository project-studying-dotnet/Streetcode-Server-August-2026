using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.Delete;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.Delete;

public class DeletePartnerHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenDeletionIsSuccessful()
    {
        int partnerId = 1;
        var partnerEntity = new Partner { Id = partnerId };
        var partnerDto = new PartnerDTO { Id = partnerId };
        var query = new DeletePartnerQuery(partnerId);

        _repositoryMock.Setup(r => r.PartnersRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Partner, bool>>>(), null))
            .ReturnsAsync(partnerEntity);
        _mapperMock.Setup(m => m.Map<PartnerDTO>(partnerEntity)).Returns(partnerDto);

        var handler = new DeletePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(partnerId, result.Value.Id);
        _repositoryMock.Verify(r => r.PartnersRepository.Delete(partnerEntity), Times.Once);
        _repositoryMock.Verify(r => r.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenPartnerNotFound()
    {
        int partnerId = 99;
        var query = new DeletePartnerQuery(partnerId);
        string expectedError = "No partner with such id";

        _repositoryMock.Setup(r => r.PartnersRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Partner, bool>>>(), null))
            .ReturnsAsync((Partner)null!);

        var handler = new DeletePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        _repositoryMock.Verify(r => r.PartnersRepository.Delete(It.IsAny<Partner>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenExceptionIsThrownOnSave()
    {
        int partnerId = 1;
        var partnerEntity = new Partner { Id = partnerId };
        var query = new DeletePartnerQuery(partnerId);
        string expectedError = "DB save failed";

        _repositoryMock.Setup(r => r.PartnersRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Partner, bool>>>(), null))
            .ReturnsAsync(partnerEntity);
        _repositoryMock.Setup(r => r.SaveChanges()).Throws(new Exception(expectedError));

        var handler = new DeletePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}