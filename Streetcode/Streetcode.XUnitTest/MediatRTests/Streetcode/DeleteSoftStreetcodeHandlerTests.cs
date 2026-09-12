using AutoMapper;
using Moq;
using Repositories.Interfaces;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.DTO.Streetcode.Create;
using Streetcode.BLL.DTO.Streetcode.Update;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.DeleteSoft;
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

public class DeleteSoftStreetcodeHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock = new();
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new();
    private readonly DeleteSoftStreetcodeHandler _handler;

    public DeleteSoftStreetcodeHandlerTests()
    {
        _repositoryMock
            .Setup(wrapper => wrapper.StreetcodeRepository)
            .Returns(_streetcodeRepositoryMock.Object);

        _repositoryMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(1);

        _handler = new DeleteSoftStreetcodeHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_SoftDeletesStreetcode_WhenStreetcodeExists()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Status = StreetcodeStatus.Published };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(), null))
            .ReturnsAsync(existingStreetcode);

        var command = new DeleteSoftStreetcodeCommand(existingStreetcodeId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StreetcodeStatus.Deleted, existingStreetcode.Status);
        _streetcodeRepositoryMock.Verify(repo => repo.Update(existingStreetcode), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenStreetcodeDoesNotExist()
    {
        var existingStreetcodeId = 1;

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(),
                null))
            .ReturnsAsync((StreetcodeEntity?)null);

        var command = new DeleteSoftStreetcodeCommand(existingStreetcodeId);

        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenSaveChangesFails()
    {
        var existingStreetcodeId = 1;
        var existingStreetcode = new PersonStreetcode { Id = existingStreetcodeId, Status = StreetcodeStatus.Published };

        _streetcodeRepositoryMock
            .Setup(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreetcodeEntity, bool>>>(), null))
            .ReturnsAsync(existingStreetcode);

        _repositoryMock
            .Setup(wrapper => wrapper.SaveChangesAsync())
            .ReturnsAsync(0);

        var command = new DeleteSoftStreetcodeCommand(existingStreetcodeId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to change status of streetcode to deleted", result.Errors.First().Message);
        _streetcodeRepositoryMock.Verify(repo => repo.Update(existingStreetcode), Times.Once);
    }
}
