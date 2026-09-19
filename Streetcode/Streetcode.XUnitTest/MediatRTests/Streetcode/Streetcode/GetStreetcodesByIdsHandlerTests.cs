using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Specifications.Streetcode.Streetcode;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Streetcode;

public class GetStreetcodesByIdsHandlerTests
{
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

    private GetStreetcodesByIdsHandler CreateHandler() =>
        new(_repositoryWrapperMock.Object, _mapperMock.Object);

    private void SetupRepository(params int[] existingIds) =>
        _repositoryWrapperMock
            .Setup(r => r.StreetcodeRepository.ListAsync(
                It.IsAny<GetPublishedStreetcodesByIdsSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIds.Select(id => new StreetcodeContent { Id = id }).ToList());

    [Fact]
    public async Task Handle_ReturnsStreetcodesInRequestedOrder()
    {
        SetupRepository(1, 2, 3);

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 3, 1, 2 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 3, 1, 2 }, result.Value.Select(dto => dto.Id));
    }

    [Fact]
    public async Task Handle_SkipsIdsThatAreNotReturnedByRepository()
    {
        SetupRepository(2);

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1, 2, 404 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(2, dto.Id);
    }

    [Fact]
    public async Task Handle_ReturnsEachStreetcodeOnce_WhenIdsAreDuplicated()
    {
        SetupRepository(1, 2);

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1, 2, 1, 2, 1 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2 }, result.Value.Select(dto => dto.Id));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoIdsAreRequested()
    {
        SetupRepository();

        var result = await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(Array.Empty<int>()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        SetupRepository(1);

        await CreateHandler().Handle(
            new GetStreetcodesByIdsQuery(new[] { 1 }),
            cancellationToken);

        _repositoryWrapperMock.Verify(
            r => r.StreetcodeRepository.ListAsync(
                It.IsAny<GetPublishedStreetcodesByIdsSpecification>(),
                cancellationToken),
            Times.Once);
    }
}
