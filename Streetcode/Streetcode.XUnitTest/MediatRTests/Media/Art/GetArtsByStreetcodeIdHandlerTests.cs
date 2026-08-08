using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Media.Art.GetByStreetcodeId;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.Media.Art;

public class GetArtsByStreetcodeIdHandlerTests
{
    private readonly Mock<IRepositoryWrapper> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;

    public GetArtsByStreetcodeIdHandlerTests()
    {
        _repositoryMock = new Mock<IRepositoryWrapper>();
        _mapperMock = new Mock<IMapper>();
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILoggerService>();
    }

    [Fact]
    public async Task Handle_ReturnsOkResult_WithBase64Images_WhenArtsExist()
    {
        int streetcodeId = 1;
        var blobName = "test-blob.jpg";
        var base64String = "base64EncodedString";

        var arts = new List<DAL.Entities.Media.Images.Art>
        {
            new DAL.Entities.Media.Images.Art { Id = 1, Image = new DAL.Entities.Media.Images.Image { BlobName = blobName } }
        };

        var artsDto = new List<ArtDTO>
        {
            new ArtDTO { Id = 1, Image = new ImageDTO { BlobName = blobName } }
        };

        _repositoryMock.Setup(r => r.ArtRepository.GetAllAsync(
                It.IsAny<Expression<Func<DAL.Entities.Media.Images.Art, bool>>>(),
                It.IsAny<Func<IQueryable<DAL.Entities.Media.Images.Art>, IIncludableQueryable<DAL.Entities.Media.Images.Art, object>>>()))
            .ReturnsAsync(arts);

        _mapperMock.Setup(m => m.Map<IEnumerable<ArtDTO>>(arts))
            .Returns(artsDto);

        _blobServiceMock.Setup(b => b.FindFileInStorageAsBase64(blobName))
            .Returns(base64String);

        var handler = new GetArtsByStreetcodeIdHandler(
            _repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var query = new GetArtsByStreetcodeIdQuery(streetcodeId);


        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var resultList = result.Value.ToList();
        Assert.Single(resultList);
        Assert.Equal(base64String, resultList.First().Image?.Base64);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_AndLogsError_WhenArtsNull()
    {
        int streetcodeId = 999;

        _repositoryMock.Setup(r => r.ArtRepository.GetAllAsync(
                It.IsAny<Expression<Func<DAL.Entities.Media.Images.Art, bool>>>(),
                It.IsAny<Func<IQueryable<DAL.Entities.Media.Images.Art>, IIncludableQueryable<DAL.Entities.Media.Images.Art, object>>>()))
            .ReturnsAsync((IEnumerable<DAL.Entities.Media.Images.Art>)null!);

        var handler = new GetArtsByStreetcodeIdHandler(
            _repositoryMock.Object, _mapperMock.Object, _blobServiceMock.Object, _loggerMock.Object);
        var query = new GetArtsByStreetcodeIdQuery(streetcodeId);
        string expectedErrorMsg = $"Cannot find any art with corresponding streetcode id: {streetcodeId}";

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(expectedErrorMsg, result.Errors.First().Message);

        _loggerMock.Verify(l => l.LogError(query, expectedErrorMsg), Times.Once);
    }
}