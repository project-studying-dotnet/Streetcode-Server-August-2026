using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Partners.Create;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.Update;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.Update;

public class UpdatePartnerHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new Mock<IRepositoryWrapper> { DefaultValue = DefaultValue.Mock };
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenUpdateIsSuccessful()
    {
        var updateDto = new CreatePartnerDTO
        {
            Id = 1,
            PartnerSourceLinks = new List<CreatePartnerSourceLinkDTO> { new() { Id = 2 } },
            Streetcodes = new List<StreetcodeShortDTO> { new() { Id = 3 } }
        };

        var partnerEntity = new Partner
        {
            Id = 1,
            PartnerSourceLinks = new List<PartnerSourceLink> { new() { Id = 2 } },
            Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent>()
        };

        var returnedPartnerDto = new PartnerDTO { Id = 1, Streetcodes = new List<StreetcodeShortDTO>() };

        var oldLinks = new List<PartnerSourceLink> { new() { Id = 1 } };
        var oldStreetcodes = new List<StreetcodePartner> { new() { StreetcodeId = 1 } };

        var query = new UpdatePartnerQuery(updateDto);

        _mapperMock.Setup(m => m.Map<Partner>(updateDto)).Returns(partnerEntity);
        _mapperMock.Setup(m => m.Map<PartnerDTO>(partnerEntity)).Returns(returnedPartnerDto);

        _repositoryMock.Setup(r => r.PartnerSourceLinkRepository.GetAllAsync(It.IsAny<Expression<Func<PartnerSourceLink, bool>>>(), null))
            .ReturnsAsync(oldLinks);
        _repositoryMock.Setup(r => r.PartnerStreetcodeRepository.GetAllAsync(It.IsAny<Expression<Func<StreetcodePartner, bool>>>(), null))
            .ReturnsAsync(oldStreetcodes);

        var handler = new UpdatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _repositoryMock.Verify(r => r.PartnerSourceLinkRepository.Delete(It.Is<PartnerSourceLink>(l => l.Id == 1)), Times.Once);
        _repositoryMock.Verify(r => r.PartnerStreetcodeRepository.Delete(It.Is<StreetcodePartner>(s => s.StreetcodeId == 1)), Times.Once);

        _repositoryMock.Verify(r => r.PartnerStreetcodeRepository.Create(It.Is<StreetcodePartner>(s => s.StreetcodeId == 3)), Times.Once);

        _repositoryMock.Verify(r => r.PartnersRepository.Update(partnerEntity), Times.Once);
        _repositoryMock.Verify(r => r.SaveChanges(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenLinksAndStreetcodesAreEmpty_EdgeCase()
    {
        var updateDto = new CreatePartnerDTO
        {
            Id = 1,
            PartnerSourceLinks = new List<CreatePartnerSourceLinkDTO>(),
            Streetcodes = new List<StreetcodeShortDTO>()
        };
        var partnerEntity = new Partner { Id = 1, Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent>() };
        var returnedPartnerDto = new PartnerDTO { Id = 1, Streetcodes = new List<StreetcodeShortDTO>() };

        var query = new UpdatePartnerQuery(updateDto);

        _mapperMock.Setup(m => m.Map<Partner>(updateDto)).Returns(partnerEntity);
        _mapperMock.Setup(m => m.Map<PartnerDTO>(partnerEntity)).Returns(returnedPartnerDto);

        _repositoryMock.Setup(r => r.PartnerSourceLinkRepository.GetAllAsync(It.IsAny<Expression<Func<PartnerSourceLink, bool>>>(), null))
            .ReturnsAsync(new List<PartnerSourceLink>());
        _repositoryMock.Setup(r => r.PartnerStreetcodeRepository.GetAllAsync(It.IsAny<Expression<Func<StreetcodePartner, bool>>>(), null))
            .ReturnsAsync(new List<StreetcodePartner>());

        var handler = new UpdatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.PartnersRepository.Update(partnerEntity), Times.Once);
        _repositoryMock.Verify(r => r.PartnerSourceLinkRepository.Delete(It.IsAny<PartnerSourceLink>()), Times.Never);
        _repositoryMock.Verify(r => r.PartnerStreetcodeRepository.Delete(It.IsAny<StreetcodePartner>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenExceptionIsThrown()
    {
        var updateDto = new CreatePartnerDTO { Id = 1 };
        var partnerEntity = new Partner();
        var query = new UpdatePartnerQuery(updateDto);
        var expectedError = "Database error during update";

        _mapperMock.Setup(m => m.Map<Partner>(updateDto)).Returns(partnerEntity);

        _repositoryMock.Setup(r => r.SaveChanges()).Throws(new Exception(expectedError));

        var handler = new UpdatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}