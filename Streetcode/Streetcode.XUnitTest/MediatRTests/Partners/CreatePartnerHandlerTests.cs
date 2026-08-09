using System.Linq.Expressions;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Partners;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Partners.Create;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Partners.Create;

public class CreatePartnerHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new Mock<IRepositoryWrapper> { DefaultValue = DefaultValue.Mock };
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenCreationIsSuccessful()
    {
        var createPartnerDto = new CreatePartnerDTO { Streetcodes = new List<StreetcodeShortDTO> { new() { Id = 1 } } };
        var partnerEntity = new Partner { Id = 1, Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent>() };
        var returnedPartnerDto = new PartnerDTO { Id = 1 };

        var query = new CreatePartnerQuery(createPartnerDto);

        _mapperMock.Setup(m => m.Map<Partner>(createPartnerDto)).Returns(partnerEntity);
        _repositoryMock.Setup(r => r.PartnersRepository.CreateAsync(partnerEntity)).ReturnsAsync(partnerEntity);
        _repositoryMock.Setup(r => r.StreetcodeRepository.GetAllAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(new List<DAL.Entities.Streetcode.StreetcodeContent> { new() { Id = 1 } });

        _mapperMock.Setup(m => m.Map<PartnerDTO>(partnerEntity)).Returns(returnedPartnerDto);

        var handler = new CreatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(returnedPartnerDto.Id, result.Value.Id);
        _repositoryMock.Verify(r => r.SaveChanges(), Times.Exactly(2));
        _loggerMock.Verify(l => l.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenStreetcodesListIsEmpty_EdgeCase()
    {
        var createPartnerDto = new CreatePartnerDTO { Streetcodes = new List<StreetcodeShortDTO>() };
        var partnerEntity = new Partner { Id = 1, Streetcodes = new List<DAL.Entities.Streetcode.StreetcodeContent>() };
        var returnedPartnerDto = new PartnerDTO { Id = 1 };

        var query = new CreatePartnerQuery(createPartnerDto);

        _mapperMock.Setup(m => m.Map<Partner>(createPartnerDto)).Returns(partnerEntity);
        _repositoryMock.Setup(r => r.PartnersRepository.CreateAsync(partnerEntity)).ReturnsAsync(partnerEntity);
        _repositoryMock.Setup(r => r.StreetcodeRepository.GetAllAsync(It.IsAny<Expression<Func<DAL.Entities.Streetcode.StreetcodeContent, bool>>>(), null))
            .ReturnsAsync(new List<DAL.Entities.Streetcode.StreetcodeContent>());
        _mapperMock.Setup(m => m.Map<PartnerDTO>(partnerEntity)).Returns(returnedPartnerDto);

        var handler = new CreatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.SaveChanges(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenExceptionIsThrown()
    {
        var createPartnerDto = new CreatePartnerDTO();
        var partnerEntity = new Partner();
        var query = new CreatePartnerQuery(createPartnerDto);
        var expectedError = "Database error";

        _mapperMock.Setup(m => m.Map<Partner>(createPartnerDto)).Returns(partnerEntity);

        _repositoryMock.Setup(r => r.PartnersRepository.CreateAsync(partnerEntity)).ThrowsAsync(new Exception(expectedError));

        var handler = new CreatePartnerHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedError, result.Errors.First().Message);
        _loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
    }
}