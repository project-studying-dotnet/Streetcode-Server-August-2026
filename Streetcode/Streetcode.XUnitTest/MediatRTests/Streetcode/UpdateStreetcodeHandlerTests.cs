using AutoMapper;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Update;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Update;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Media;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using StreetcodeEntity = Streetcode.DAL.Entities.Streetcode.StreetcodeContent;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode;

public class UpdateStreetcodeHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly Mock<ITagRepository> _tagRepositoryMock = new();
    private readonly Mock<IStreetcodeImageRepository> _streetcodeImageRepositoryMock = new();
    private readonly Mock<IImageRepository> _imageRepositoryMock = new();
    private readonly Mock<IAudioRepository> _audioRepositoryMock = new();
    private readonly UpdateStreetcodeHandler _handler;

    public UpdateStreetcodeHandlerTests()
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

        _handler = new UpdateStreetcodeHandler(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenPersonUpdateIsSuccessful()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WhenEventUpdateIsSuccessful()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new EventStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Event, null, null);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAnimationImageIsNotGif()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var badImage = new Image { Id = 1, MimeType = "image/jpeg" };
        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .ReturnsAsync(badImage);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, 1, null);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Animation image must be a GIF file.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAudioFileIsNotMp3()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var badAudio = new Audio { Id = 1, MimeType = "audio/wav" };
        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Audio, bool>>>()))
            .ReturnsAsync(badAudio);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, 1);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Audio must be an MP3 file.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenStreetcodeDoesNotExist()
    {
        var presumablyExistingStreetcodeId = 1;

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync((StreetcodeEntity?)null);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        var command = new UpdateStreetcodeCommand(presumablyExistingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal($"Cannot find a streetcode with id: {presumablyExistingStreetcodeId}", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTypeOfTheStreetcodeDoesNotMatch()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Event, null, null);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Streetcode type cannot be changed after creation.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    private static UpdateStreetcodeDTO UpdateStreetcodeBuildDto(StreetcodeType streetcodeType, int? animationImageId, int? audioId)
    {
        return new UpdateStreetcodeDTO
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
