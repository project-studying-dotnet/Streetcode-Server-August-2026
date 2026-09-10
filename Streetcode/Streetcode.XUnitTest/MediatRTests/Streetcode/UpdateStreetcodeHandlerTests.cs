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
    private readonly Mock<IStreetcodeTagIndexRepository> _streetcodeTagIndexRepositoryMock = new();
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

        _repositoryMock
            .Setup(wrapper => wrapper.StreetcodeTagIndexRepository)
            .Returns(_streetcodeTagIndexRepositoryMock.Object);

        _streetcodeTagIndexRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeTagIndex, object>>>()))
            .ReturnsAsync(new List<StreetcodeTagIndex>());

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
    public async Task Handle_ReturnsFailedResult_WhenTagIsNotFound()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        updateStreetcodeDTO.Tags = new List<StreetcodeTagDTO>
        {
            new StreetcodeTagDTO { Id = 999, Title = "Missing", IsVisible = true, Index = 0 },
        };

        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Tag(s) not found: 999", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAnimationImageIsNotFound()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .ReturnsAsync((Image?)null);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, 1, null);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Animation image not found.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenBlackAndWhiteImageIsNotFound()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .ReturnsAsync((Image?)null);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null, blackAndWhiteImageId: 1);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Black and white image not found.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenRelatedFigureImageIsNotFound()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .ReturnsAsync((Image?)null);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null, relatedFigureImageId: 1);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Related figure image not found.", result.Errors.First().Message);
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenAudioIsNotFound()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Audio, bool>>>()))
            .ReturnsAsync((Audio?)null);

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, 1);
        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal("Audio not found.", result.Errors.First().Message);
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

    [Fact]
    public async Task Handle_WhenTagIsAlreadyLinked_ShouldUpdateItInsteadOfDuplicating()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        _tagRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>(),
                It.IsAny<Func<IQueryable<Tag>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Tag, object>>>()))
            .ReturnsAsync(new List<Tag> { new Tag { Id = 5, Title = "Existing" } });

        var alreadyLinkedTagIndex = new StreetcodeTagIndex
        {
            StreetcodeId = existingStreetcodeId,
            TagId = 5,
            IsVisible = false,
            Index = 0,
        };

        _streetcodeTagIndexRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeTagIndex, object>>>()))
            .ReturnsAsync(new List<StreetcodeTagIndex> { alreadyLinkedTagIndex });

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        updateStreetcodeDTO.Tags = new List<StreetcodeTagDTO>
        {
            new StreetcodeTagDTO { Id = 5, Title = "Existing", IsVisible = true, Index = 2 },
        };

        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        _streetcodeTagIndexRepositoryMock.Verify(
            repo => repo.Create(It.IsAny<StreetcodeTagIndex>()),
            Times.Never);
        _streetcodeTagIndexRepositoryMock.Verify(
            repo => repo.Update(It.Is<StreetcodeTagIndex>(ti => ti.TagId == 5 && ti.IsVisible == true && ti.Index == 2)),
            Times.Once);
        _streetcodeTagIndexRepositoryMock.Verify(
            repo => repo.DeleteRange(It.Is<IEnumerable<StreetcodeTagIndex>>(items => !items.Any())),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTagIsRemovedFromRequest_ShouldDeleteItsIndex()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var tagIndexToRemove = new StreetcodeTagIndex
        {
            StreetcodeId = existingStreetcodeId,
            TagId = 7,
            IsVisible = true,
            Index = 0,
        };

        _streetcodeTagIndexRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeTagIndex, object>>>()))
            .ReturnsAsync(new List<StreetcodeTagIndex> { tagIndexToRemove });

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(StreetcodeType.Person, null, null);
        updateStreetcodeDTO.Tags = new List<StreetcodeTagDTO>();

        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        _streetcodeTagIndexRepositoryMock.Verify(
            repo => repo.DeleteRange(It.Is<IEnumerable<StreetcodeTagIndex>>(items => items.Contains(tagIndexToRemove))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAllRoleImagesTagsAndAudioProvided_ShouldUpdateSuccessfullyWithAllOfThem()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Tags = new List<Tag>() };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeEntity, object>>>()))
            .ReturnsAsync(existingStreetcode);

        var images = new List<Image>
        {
            new Image { Id = 1, MimeType = "image/gif" },
            new Image { Id = 2, MimeType = "image/jpeg" },
            new Image { Id = 3, MimeType = "image/png" },
        };

        _imageRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Image, bool>>>()))
            .Returns((System.Linq.Expressions.Expression<Func<Image, bool>> predicate, Func<IQueryable<Image>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Image, object>>? include) =>
                Task.FromResult(images.AsQueryable().FirstOrDefault(predicate)));

        var audio = new Audio { Id = 4, MimeType = "audio/mpeg" };
        _audioRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Audio, bool>>>()))
            .Returns((System.Linq.Expressions.Expression<Func<Audio, bool>> predicate, Func<IQueryable<Audio>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Audio, object>>? include) =>
                Task.FromResult(new[] { audio }.AsQueryable().FirstOrDefault(predicate)));

        _tagRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>(),
                It.IsAny<Func<IQueryable<Tag>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Tag, object>>>()))
            .ReturnsAsync(new List<Tag> { new Tag { Id = 5, Title = "Existing" } });

        _streetcodeImageRepositoryMock
            .Setup(repo => repo.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeImage, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeImage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<StreetcodeImage, object>>>()))
            .ReturnsAsync(new List<StreetcodeImage>());

        var updateStreetcodeDTO = UpdateStreetcodeBuildDto(
            StreetcodeType.Person,
            animationImageId: 1,
            audioId: 4,
            blackAndWhiteImageId: 2,
            relatedFigureImageId: 3);
        updateStreetcodeDTO.Tags = new List<StreetcodeTagDTO>
        {
            new StreetcodeTagDTO { Id = 5, Title = "Existing", IsVisible = true, Index = 2 },
        };

        var command = new UpdateStreetcodeCommand(existingStreetcodeId, updateStreetcodeDTO);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(e => e.Message)));
        _repositoryMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once);

        _streetcodeImageRepositoryMock.Verify(
            repo => repo.CreateRangeAsync(It.Is<IEnumerable<StreetcodeImage>>(items =>
                items.Count() == 3 &&
                items.Any(i => i.ImageAssigment == ImageAssigment.Animation && i.Image!.Id == 1) &&
                items.Any(i => i.ImageAssigment == ImageAssigment.Blackandwhite && i.Image!.Id == 2) &&
                items.Any(i => i.ImageAssigment == ImageAssigment.Relatedfigure && i.Image!.Id == 3))),
            Times.Once);

        _streetcodeTagIndexRepositoryMock.Verify(
            repo => repo.Create(It.Is<StreetcodeTagIndex>(ti => ti.TagId == 5 && ti.IsVisible == true && ti.Index == 2)),
            Times.Once);
    }

    private static UpdateStreetcodeDTO UpdateStreetcodeBuildDto(
        StreetcodeType streetcodeType,
        int? animationImageId,
        int? audioId,
        int? blackAndWhiteImageId = null,
        int? relatedFigureImageId = null)
    {
        return new UpdateStreetcodeDTO
        {
            Index = 1,
            Title = "Test Streetcode",
            ShortDescription = "Test short description.",
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
            BlackAndWhiteImageId = blackAndWhiteImageId,
            RelatedFigureImageId = relatedFigureImageId,
            AudioId = audioId,
        };
    }
}
