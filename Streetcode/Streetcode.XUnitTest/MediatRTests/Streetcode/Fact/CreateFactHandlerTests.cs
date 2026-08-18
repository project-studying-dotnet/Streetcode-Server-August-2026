using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Xunit;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Fact.Create;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;
using ImageEntity = Streetcode.DAL.Entities.Media.Images.Image;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class CreateFactHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IFactRepository> _factRepositoryMock = new();
    private readonly Mock<IImageDetailsRepository> _imageDetailsRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerServiceMock = new();

    public CreateFactHandlerTests()
    {
        _repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageDetailsRepository)
            .Returns(_imageDetailsRepositoryMock.Object);

        _repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(_factRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
    {
        var factDto = new FactUpdateCreateDto
        {
            Title = "Test fact",
            FactContent = "Test content",
            ImageId = 5,
            StreetcodeId = 10,
        };
        var command = new CreateFactCommand(factDto);
        var expectedMessage = "Cannot find streetcode with id: 10";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync((StreetcodeEntity?)null);

        var handler = new CreateFactHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());

        _imageRepositoryMock.VerifyNoOtherCalls();
        _factRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        _imageDetailsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var factDto = new FactUpdateCreateDto
        {
            Title = "Test fact",
            FactContent = "Test content",
            ImageId = 5,
            StreetcodeId = 10,
        };

        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };

        const string expectedMessage = "Cannot find image with id: 5";

        _streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        _imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<ImageEntity>,
                    IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity?)null);

        var handler = new CreateFactHandler(
            _repositoryWrapperMock.Object,
            _mapperMock.Object,
            _loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        _loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        _factRepositoryMock.VerifyNoOtherCalls();
        _imageDetailsRepositoryMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();

        _repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
    }
}
