// <copyright file="CreateFactHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.Create;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Media.Images;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Repositories.Interfaces;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;
    using ImageDetailsEntity = global::Streetcode.DAL.Entities.Media.Images.ImageDetails;
    using ImageEntity = global::Streetcode.DAL.Entities.Media.Images.Image;
    using StreetcodeEntity = global::Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

    public class CreateFactHandlerTests
    {
    private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
    private readonly Mock<IStreetcodeRepository> streetcodeRepositoryMock = new ();
    private readonly Mock<IImageRepository> imageRepositoryMock = new ();
    private readonly Mock<IFactRepository> factRepositoryMock = new ();
    private readonly Mock<IImageDetailsRepository> imageDetailsRepositoryMock = new ();
    private readonly Mock<IMapper> mapperMock = new ();
    private readonly Mock<ILoggerService> loggerServiceMock = new ();

    public CreateFactHandlerTests()
    {
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(this.streetcodeRepositoryMock.Object);

        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(this.imageRepositoryMock.Object);

        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.ImageDetailsRepository)
            .Returns(this.imageDetailsRepositoryMock.Object);

        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.FactRepository)
            .Returns(this.factRepositoryMock.Object);
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

        this.streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync((StreetcodeEntity?)null);

        var handler = new CreateFactHandler(
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object,
            this.loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());

        this.imageRepositoryMock.VerifyNoOtherCalls();
        this.factRepositoryMock.VerifyNoOtherCalls();
        this.mapperMock.VerifyNoOtherCalls();

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        this.imageDetailsRepositoryMock.VerifyNoOtherCalls();
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

        this.streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);

        this.imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<
                    IQueryable<ImageEntity>,
                    IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync((ImageEntity?)null);

        var handler = new CreateFactHandler(
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object,
            this.loggerServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);

        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());

        this.factRepositoryMock.VerifyNoOtherCalls();
        this.imageDetailsRepositoryMock.VerifyNoOtherCalls();
        this.mapperMock.VerifyNoOtherCalls();

        this.repositoryWrapperMock.Verify(
            wrapper => wrapper.SaveChangesAsync(),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenSavingFails_ShouldReturnFailure()
    {
        var factDto = CreateFactDto(title: "  Test fact  ", factContent: "  Test content  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var fact = new FactEntity();
        const string expectedMessage = "Failed to create fact";

        this.SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedMessage, result.Errors.Single().Message);
        Assert.Equal(1, fact.DisplayOrder);
        Assert.Equal("Test fact", fact.Title);
        Assert.Equal("Test content", fact.FactContent);

        this.factRepositoryMock.Verify(repository => repository.CreateAsync(fact), Times.Once());
        this.mapperMock.Verify(mapper => mapper.Map<FactDto>(It.IsAny<object>()), Times.Never());
        this.loggerServiceMock.Verify(
            logger => logger.LogError(command, expectedMessage),
            Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDataIsValid_ShouldCreateFactWithNextDisplayOrder()
    {
        var factDto = CreateFactDto();
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var existingFacts = new[]
        {
            new FactEntity { DisplayOrder = 1 },
            new FactEntity { DisplayOrder = 3 },
        };
        var fact = new FactEntity();
        var createdFactDto = new FactDto { Id = 15 };

        this.SetupValidDependencies(factDto, streetcode, image, fact, existingFacts);
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(createdFactDto, result.Value);
        Assert.Equal(4, fact.DisplayOrder);
        Assert.Null(result.Value.ImageAlt);

        this.factRepositoryMock.Verify(repository => repository.CreateAsync(fact), Times.Once());
        this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        this.imageDetailsRepositoryMock.VerifyNoOtherCalls();
        this.loggerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenImageAltProvidedAndDetailsDoNotExist_ShouldCreateImageDetails()
    {
        var factDto = CreateFactDto(imageAlt: "  Accessible description  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var image = new ImageEntity { Id = factDto.ImageId };
        var fact = new FactEntity();
        var createdFactDto = new FactDto();
        ImageDetailsEntity? createdImageDetails = null;

        this.SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        this.imageDetailsRepositoryMock
            .Setup(repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()))
            .Callback<ImageDetailsEntity>(details => createdImageDetails = details)
            .Returns((ImageDetailsEntity details) => Task.FromResult(details));
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdImageDetails);
        Assert.Same(createdImageDetails, image.ImageDetails);
        Assert.Equal(image.Id, createdImageDetails.ImageId);
        Assert.Equal("Accessible description", createdImageDetails.Alt);
        Assert.Equal("Accessible description", result.Value.ImageAlt);

        this.imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(createdImageDetails),
            Times.Once());
        this.imageDetailsRepositoryMock.Verify(
            repository => repository.Update(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WhenImageDetailsExist_ShouldUpdateDescription()
    {
        var factDto = CreateFactDto(imageAlt: "  Updated description  ");
        var command = new CreateFactCommand(factDto);
        var streetcode = new StreetcodeEntity { Id = factDto.StreetcodeId };
        var imageDetails = new ImageDetailsEntity { ImageId = factDto.ImageId, Alt = "Old description" };
        var image = new ImageEntity { Id = factDto.ImageId, ImageDetails = imageDetails };
        var fact = new FactEntity();
        var createdFactDto = new FactDto();

        this.SetupValidDependencies(factDto, streetcode, image, fact, Array.Empty<FactEntity>());
        this.repositoryWrapperMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactDto>(fact))
            .Returns(createdFactDto);

        var result = await this.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated description", imageDetails.Alt);
        Assert.Equal("Updated description", result.Value.ImageAlt);

        this.imageDetailsRepositoryMock.Verify(repository => repository.Update(imageDetails), Times.Once());
        this.imageDetailsRepositoryMock.Verify(
            repository => repository.CreateAsync(It.IsAny<ImageDetailsEntity>()),
            Times.Never());
    }

    private static FactUpdateCreateDto CreateFactDto(
        string title = "Test fact",
        string factContent = "Test content",
        string? imageAlt = null) =>
        new ()
        {
            Title = title,
            FactContent = factContent,
            ImageAlt = imageAlt,
            ImageId = 5,
            StreetcodeId = 10,
        };

    private CreateFactHandler CreateHandler() =>
        new (
            this.repositoryWrapperMock.Object,
            this.mapperMock.Object,
            this.loggerServiceMock.Object);

    private void SetupValidDependencies(
        FactUpdateCreateDto factDto,
        StreetcodeEntity streetcode,
        ImageEntity image,
        FactEntity fact,
        IEnumerable<FactEntity> existingFacts)
    {
        this.streetcodeRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync(streetcode);
        this.imageRepositoryMock
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<ImageEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ImageEntity>, IIncludableQueryable<ImageEntity, object>>?>()))
            .ReturnsAsync(image);
        this.factRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<FactEntity, bool>>>(),
                null))
            .ReturnsAsync(existingFacts);
        this.mapperMock
            .Setup(mapper => mapper.Map<FactEntity>(factDto))
            .Returns(fact);
        this.factRepositoryMock
            .Setup(repository => repository.CreateAsync(fact))
            .ReturnsAsync(fact);
    }
    }
}
