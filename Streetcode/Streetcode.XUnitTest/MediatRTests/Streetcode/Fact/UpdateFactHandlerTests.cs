// <copyright file="UpdateFactHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.Update;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Media.Images;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Repositories.Interfaces;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;
    using ImageDetailsEntity = global::Streetcode.DAL.Entities.Media.Images.ImageDetails;
    using ImageEntity = global::Streetcode.DAL.Entities.Media.Images.Image;

    public class UpdateFactHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<IImageRepository> imageRepositoryMock = new ();
    private readonly Mock<IImageDetailsRepository> imageDetailsRepositoryMock = new ();
    private readonly Mock<IMapper> mapperMock = new ();
    private readonly Mock<ILoggerService> loggerServiceMock = new ();

    public UpdateFactHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(this.imageRepositoryMock.Object);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageDetailsRepository)
            .Returns(this.imageDetailsRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFactDoesNotExist_ShouldReturnFailure()
    {
        var factDto = CreateFactDto();
        var command = new UpdateFactCommand(15, factDto);
        const string expectedMessage = "Cannot find fact with id: 15";

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync((FactEntity?)null);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.imageRepositoryMock.VerifyNoOtherCalls();
        this.mapperMock.VerifyNoOtherCalls();
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenStreetcodeIdChanges_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(streetcodeId: 20);
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = 10 };
        const string expectedMessage = "Cannot move fact with id 15 to another streetcode";

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.imageRepositoryMock.VerifyNoOtherCalls();
        this.mapperMock.VerifyNoOtherCalls();
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExist_ShouldReturnFailure()
    {
        var factDto = CreateFactDto();
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        const string expectedMessage = "Cannot find image with id: 5";

        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        this.imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity?)null);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
        this.mapperMock.VerifyNoOtherCalls();
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(title: "  Updated fact  ", factContent: "  Updated content  ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        const string expectedMessage = "Failed to update fact with id: 15";

        this.SetupValidDependencies(factDto, fact, image);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        Assert.Equal("Updated fact", fact.Title);
        Assert.Equal("Updated content", fact.FactContent);

        this.factRepositoryMock.Verify(repository => repository.Update(fact), Times.Once());
        this.mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDescriptionProvidedAndDetailsDoNotExist_ShouldCreateImageDetails()
    {
        var factDto = CreateFactDto(imageAlt: "  New description  ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var resultDto = new FactDto { Id = command.Id };
        ImageDetailsEntity? createdImageDetails = null;

        this.SetupValidDependencies(factDto, fact, image);
        this.imageDetailsRepositoryMock
            .Setup(repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()))
            .Callback<ImageDetailsEntity>(details => createdImageDetails = details)
            .Returns((ImageDetailsEntity details) => Task.FromResult(details));
        this.SetupSuccessfulSave(fact, resultDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(resultDto, result.Value);
        Assert.NotNull(createdImageDetails);
        Assert.Same(createdImageDetails, image.ImageDetails);
        Assert.Equal("New description", createdImageDetails.Alt);
        Assert.Equal("New description", result.Value.ImageAlt);

        this.imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(createdImageDetails),
            Times.Once());
        this.imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageAltIsWhitespaceAndDetailsExist_ShouldClearAlt()
    {
        var factDto = CreateFactDto(imageAlt: "   ");
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Old description" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var resultDto = new FactDto { Id = command.Id };

        this.SetupValidDependencies(factDto, fact, image);
        this.SetupSuccessfulSave(fact, resultDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(imageDetails.Alt);
        Assert.Null(result.Value.ImageAlt);

        this.imageDetailsRepositoryMock.Verify(repository => repository.Update(imageDetails), Times.Once());
        this.imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageAltIsNull_ShouldNotTouchImageDetails()
    {
        var factDto = CreateFactDto(imageAlt: null);
        var command = new UpdateFactCommand(15, factDto);
        var fact = new FactEntity { Id = command.Id, StreetcodeId = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Existing alt" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var resultDto = new FactDto { Id = command.Id };

        this.SetupValidDependencies(factDto, fact, image);
        this.SetupSuccessfulSave(fact, resultDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Existing alt", imageDetails.Alt);

        this.imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
        this.imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    private static FactUpdateCreateDto CreateFactDto(
        string title = "Updated fact",
        string factContent = "Updated content",
        string? imageAlt = null,
        int streetcodeId = 10) =>
        new ()
        {
            Title = title,
            FactContent = factContent,
            ImageAlt = imageAlt,
            ImageId = 5,
            StreetcodeId = streetcodeId,
        };

    private UpdateFactHandler CreateHandler() =>
        new (
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object,
            this.loggerServiceMock.Object);

    private void SetupValidDependencies(
        FactUpdateCreateDto factDto,
        FactEntity fact,
        ImageEntity image)
    {
        this.factRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(fact);
        this.imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
        this.mapperMock
            .Setup(mapper => mapper.Map(factDto, fact))
            .Returns(fact);
    }

    private void SetupSuccessfulSave(FactEntity fact, FactDto resultDto)
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(resultDto);
    }
    }
}
