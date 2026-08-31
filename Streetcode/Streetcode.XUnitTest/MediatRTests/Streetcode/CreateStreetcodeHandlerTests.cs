using AutoMapper;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Create;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Media;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode;

public class CreateStreetcodeHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<ITagRepository> _tagRepositoryMock = new();
    private readonly Mock<IStreetcodeImageRepository> _streetcodeImageRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IAudioRepository> _audioRepositoryMock = new();
    private readonly CreateStreetcodeHandler _handler;

    public CreateStreetcodeHandlerTests()
    {
        _repositoryMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _repositoryMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        _repositoryMock
            .Setup(wrapper => wrapper.TagRepository)
            .Returns(_tagRepositoryMock.Object);

        _tagRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>(),
                It.IsAny<Func<IQueryable<Tag>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Tag, object>>>()))
            .ReturnsAsync(new List<Tag>());

        _repositoryMock
            .Setup(wrapper => wrapper.StreetcodeImageRepository)
            .Returns(_streetcodeImageRepositoryMock.Object);

        _streetcodeImageRepositoryMock
            .Setup(repo => repo.CreateRangeAsync(It.IsAny<IEnumerable<StreetcodeImage>>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(wrapper => wrapper.ImageRepository)
            .Returns(_imageRepositoryMock.Object);

        _repositoryMock
            .Setup(wrapper => wrapper.AudioRepository)
            .Returns(_audioRepositoryMock.Object);

        _mapperMock
            .Setup(m => m.Map<StreetcodeDTO>(It.IsAny<StreetcodeEntity>()))
            .Returns(new StreetcodeDTO { Id = 1, Title = "Test Streetcode" });

        _handler = new CreateStreetcodeHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPersonCreationIsSuccessful()
    {
        var createStreetcodeDTO = CreateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        var command = new CreateStreetcodeCommand(createStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once);
        _loggerMock.Verify(logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        _mapperMock.Verify(mapper => mapper.Map<StreetcodeDTO>(It.Is<StreetcodeEntity>(s => s is PersonStreetcode)), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenEventCreationIsSuccessful()
    {
        var createStreetcodeDTO = CreateStreetcodeBuildDto(StreetcodeType.Event, null, null);
        var command = new CreateStreetcodeCommand(createStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once);
        _loggerMock.Verify(logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        _mapperMock.Verify(mapper => mapper.Map<StreetcodeDTO>(It.Is<StreetcodeEntity>(s => s is EventStreetcode)), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAnimationImageIsNotGif()
    {
        var badImage = new Image { Id = 1, MimeType = "image/jpeg" };
        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .ReturnsAsync(badImage);

        var createStreetcodeDTO = CreateStreetcodeBuildDto(StreetcodeType.Person, 1, null);
        var command = new CreateStreetcodeCommand(createStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Animation image must be a GIF file.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAudioFileIsNotMp3()
    {
        var badAudio = new Audio { Id = 1, MimeType = "audio/wav" };
        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Audio, bool>>>()))
            .ReturnsAsync(badAudio);

        var createStreetcodeDTO = CreateStreetcodeBuildDto(StreetcodeType.Person, null, 1);
        var command = new CreateStreetcodeCommand(createStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Audio must be an MP3 file.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    private static CreateStreetcodeDTO CreateStreetcodeBuildDto(StreetcodeType streetcodeType, int? animationImageId, int? audioId)
    {
        return new CreateStreetcodeDTO
        {
            Index = 1,
            Title = "Test Streetcode",
            StreetcodeType = streetcodeType,
            FirstName = streetcodeType == StreetcodeType.Person ? "John" : null,
            LastName = streetcodeType == StreetcodeType.Person ? "Doe" : null,
            EventStartOrPersonBirthDate = new DateTime(1990, 1, 1),
            EventEndOrPersonDeathDate = new DateTime(2020, 1, 1),
            DateString = "1990-2020",
            Teaser = "This is a test streetcode.",
            TransliterationUrl = "test-streetcode",
            Tags = new List<StreetcodeTagDTO>(),
            AnimationImageId = animationImageId,
            BlackAndWhiteImageId = null,
            RelatedFigureImageId = null,
            AudioId = audioId,
        };
    }
}
