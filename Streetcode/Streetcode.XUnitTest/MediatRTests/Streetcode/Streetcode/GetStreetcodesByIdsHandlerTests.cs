using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Specifications.Streetcode.Streetcode;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodesByIdsHandlerTests
{
    private const int DecoyStreetcodeId = 900;

    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    public GetStreetcodesByIdsHandlerTests()
    {
        _mapperMock
            .Setup(m => m.Map<IEnumerable<RelatedFigureDTO>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<StreetcodeContent>)source)
                .Select(streetcode => new RelatedFigureDTO { Id = streetcode.Id })
                .ToList());
    }

    public static IEnumerable<object[]> ImageRowOrders() => new[]
    {
        new object[] { new[] { 0, 1, 2, 3 } },
        new object[] { new[] { 3, 2, 1, 0 } },
        new object[] { new[] { 2, 0, 3, 1 } },
        new object[] { new[] { 1, 3, 0, 2 } },
    };

    private static StreetcodeImage Image(int streetcodeId, int imageId, ImageAssignment? assignment) =>
        new() { StreetcodeId = streetcodeId, ImageId = imageId, ImageAssignment = assignment };

    private static bool SelectsRequestedStreetcodes(
        GetPublishedStreetcodesByIdsSpecification specification,
        IReadOnlyCollection<int> requestedIds)
    {
        var candidates = requestedIds
            .SelectMany(id => new[]
            {
                new StreetcodeContent { Id = id, Status = StreetcodeStatus.Published },
                new StreetcodeContent { Id = id, Status = StreetcodeStatus.Draft },
                new StreetcodeContent { Id = id, Status = StreetcodeStatus.Deleted },
            })
            .Append(new StreetcodeContent { Id = DecoyStreetcodeId, Status = StreetcodeStatus.Published })
            .ToList();

        return specification.Evaluate(candidates)
            .Select(streetcode => streetcode.Id)
            .OrderBy(id => id)
            .SequenceEqual(requestedIds.OrderBy(id => id));
    }

    private static bool SelectsStreetcodeImages(
        GetRelatedFigureImagesByStreetcodeIdsSpecification specification,
        IReadOnlyCollection<int> returnedIds,
        IReadOnlyCollection<int> notReturnedIds)
    {
        var candidates = returnedIds
            .Concat(notReturnedIds)
            .Append(DecoyStreetcodeId)
            .Select(id => Image(id, id, ImageAssignment.RelatedFigure))
            .ToList();

        return specification.Evaluate(candidates)
            .Select(streetcodeImage => streetcodeImage.StreetcodeId)
            .OrderBy(id => id)
            .SequenceEqual(returnedIds.OrderBy(id => id));
    }

    private GetStreetcodesByIdsHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _mapperMock.Object);

    private void SetupRepositories(
        int[] requestedIds,
        int[] existingIds,
        params StreetcodeImage[] streetcodeImages)
    {
        var distinctIds = requestedIds.Distinct().ToList();
        var returnedIds = existingIds.Distinct().ToList();
        var notReturnedIds = distinctIds.Except(returnedIds).ToList();

        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.ListAsync(
                It.Is<GetPublishedStreetcodesByIdsSpecification>(spec => SelectsRequestedStreetcodes(spec, distinctIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIds.Select(id => new StreetcodeContent { Id = id }).ToList());
        _repositoryWrapperMock
            .Setup(r => r.StreetcodeImageRepository.ListAsync(
                It.Is<GetRelatedFigureImagesByStreetcodeIdsSpecification>(spec => SelectsStreetcodeImages(spec, returnedIds, notReturnedIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(streetcodeImages.ToList());
    }

    [Fact]
    public async Task Handle_ReturnsStreetcodesInRequestedOrder()
    {
        int[] requestedIds = { 3, 1, 2 };
        SetupRepositories(requestedIds, new[] { 1, 2, 3 });

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(requestedIds),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 3, 1, 2 }, result.Value.Select(dto => dto.Id));
    }

    [Fact]
    public async Task Handle_SkipsIdsThatAreNotReturnedByRepository()
    {
        int[] requestedIds = { 1, 2, 404 };
        SetupRepositories(requestedIds, new[] { 2 });

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(requestedIds),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(2, dto.Id);
    }

    [Fact]
    public async Task Handle_ReturnsEachStreetcodeOnce_WhenIdsAreDuplicated()
    {
        int[] requestedIds = { 1, 2, 1, 2, 1 };
        SetupRepositories(requestedIds, new[] { 1, 2 });

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(requestedIds),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2 }, result.Value.Select(dto => dto.Id));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoIdsAreRequested()
    {
        SetupRepositories(Array.Empty<int>(), Array.Empty<int>());

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(Array.Empty<int>()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepositories()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        int[] ids = { 1 };
        SetupRepositories(ids, ids);

        await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(ids),
            cancellationToken);

        _repositoryWrapperMock.Verify(
            r => r.StreetcodeRepository.ListAsync(
                It.Is<GetPublishedStreetcodesByIdsSpecification>(spec => SelectsRequestedStreetcodes(spec, ids)),
                cancellationToken),
            Times.Once);
        _repositoryWrapperMock.Verify(
            r => r.StreetcodeImageRepository.ListAsync(
                It.Is<GetRelatedFigureImagesByStreetcodeIdsSpecification>(spec => SelectsStreetcodeImages(spec, ids, Array.Empty<int>())),
                cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(ImageRowOrders))]
    public async Task Handle_UsesRelatedFigureImage_RegardlessOfImageRowOrder(int[] order)
    {
        var imageRows = new[]
        {
            Image(1, 30, ImageAssignment.Animation),
            Image(1, 31, ImageAssignment.BlackAndWhite),
            Image(1, 5, ImageAssignment.RelatedFigure),
            Image(1, 40, null),
        };
        SetupRepositories(new[] { 1 }, new[] { 1 }, order.Select(index => imageRows[index]).ToArray());

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1 }),
            CancellationToken.None);

        var dto = Assert.Single(result.Value);
        Assert.Equal(5, dto.ImageId);
    }

    [Fact]
    public async Task Handle_UsesNewestUnassignedImage_WhenStreetcodeHasNoRelatedFigureImage()
    {
        SetupRepositories(
            new[] { 1 },
            new[] { 1 },
            Image(1, 3, null),
            Image(1, 7, null));

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1 }),
            CancellationToken.None);

        var dto = Assert.Single(result.Value);
        Assert.Equal(7, dto.ImageId);
    }

    [Fact]
    public async Task Handle_UsesNewestRelatedFigureImage_WhenSeveralAreAssigned()
    {
        SetupRepositories(
            new[] { 1 },
            new[] { 1 },
            Image(1, 9, ImageAssignment.RelatedFigure),
            Image(1, 4, ImageAssignment.RelatedFigure));

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1 }),
            CancellationToken.None);

        var dto = Assert.Single(result.Value);
        Assert.Equal(9, dto.ImageId);
    }

    [Fact]
    public async Task Handle_ReturnsZeroImageId_WhenStreetcodeHasNoCardImage()
    {
        SetupRepositories(new[] { 1 }, new[] { 1 });

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1 }),
            CancellationToken.None);

        var dto = Assert.Single(result.Value);
        Assert.Equal(0, dto.ImageId);
    }

    [Fact]
    public async Task Handle_LoadsImagesOnlyForReturnedStreetcodes()
    {
        int[] requestedIds = { 1, 2, 3 };
        SetupRepositories(requestedIds, new[] { 2 }, Image(2, 20, ImageAssignment.RelatedFigure));

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(requestedIds),
            CancellationToken.None);

        var dto = Assert.Single(result.Value);
        Assert.Equal((2, 20), (dto.Id, dto.ImageId));
    }

    [Fact]
    public async Task Handle_AssignsEachImageToItsOwnStreetcode()
    {
        SetupRepositories(
            new[] { 2, 1 },
            new[] { 1, 2 },
            Image(1, 10, ImageAssignment.RelatedFigure),
            Image(2, 20, ImageAssignment.RelatedFigure));

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 2, 1 }),
            CancellationToken.None);

        Assert.Equal(new[] { (2, 20), (1, 10) }, result.Value.Select(dto => (dto.Id, dto.ImageId)));
    }
}
