using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap.Get
{
    public class GetHistoryMapRecordHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public GetHistoryMapRecordHandlerTests()
        {
            repositoryMock = new Mock<IRepositoryWrapper>();
            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILoggerService>();
        }

        [Fact]
        public async Task Handle_StreetcodeExists_ShouldReturnMappedRecords()
        {
            int streetcodeId = 1;

            var query = new GetHistoryMapRecordsByStreetcodeQuery(streetcodeId);

            var dbRecords = new List<HistoryMapRecord>
            {
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = streetcodeId,
                    ToponymId = 10,
                    PhysicalStreetcodeNumber = 1,
                },
                new HistoryMapRecord
                {
                    Id = 2,
                    StreetcodeId = streetcodeId,
                    ToponymId = 20,
                    PhysicalStreetcodeNumber = 2,
                },
            };

            var expectedDtos = new List<HistoryMapRecordDTO>
            {
                new HistoryMapRecordDTO
                {
                    Id = 1,
                    StreetcodeId = streetcodeId,
                    ToponymId = 10,
                    PhysicalStreetcodeNumber = 1,
                },
                new HistoryMapRecordDTO
                {
                    Id = 2,
                    StreetcodeId = streetcodeId,
                    ToponymId = 20,
                    PhysicalStreetcodeNumber = 2,
                },
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(streetcodeId))
                .ReturnsAsync(dbRecords);

            mapperMock.Setup(m => m.Map<IEnumerable<HistoryMapRecordDTO>>(dbRecords))
                .Returns(expectedDtos);

            var handler = new GetHistoryMapRecordsByStreetcodeHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);

            Assert.Equal(expectedDtos.Count(), result.Value.Count());
            Assert.Equal(expectedDtos, result.Value);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(streetcodeId), Times.Once);

            mapperMock.Verify(m => m.Map<IEnumerable<HistoryMapRecordDTO>>(dbRecords), Times.Once);

            loggerMock.Verify(l => l.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StreetcodeExistsWithoutRecords_ShouldReturnEmptyCollection()
        {
            int streetcodeId = 1;

            var query = new GetHistoryMapRecordsByStreetcodeQuery(streetcodeId);

            var emptyRecords = new List<HistoryMapRecord>();
            var emptyDtos = new List<HistoryMapRecordDTO>();

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(streetcodeId))
                .ReturnsAsync(emptyRecords);

            mapperMock.Setup(m => m.Map<IEnumerable<HistoryMapRecordDTO>>(emptyRecords))
                .Returns(emptyDtos);

            var handler = new GetHistoryMapRecordsByStreetcodeHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);

            Assert.Empty(result.Value);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(streetcodeId), Times.Once);

            mapperMock.Verify(m => m.Map<IEnumerable<HistoryMapRecordDTO>>(emptyRecords), Times.Once);
        }

        [Fact]
        public async Task Handle_StreetcodeNotFound_ShouldReturnfailResult()
        {
            int streetcodeId = 99;

            var query = new GetHistoryMapRecordsByStreetcodeQuery(streetcodeId);

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync((StreetcodeContent)null!);

            var handler = new GetHistoryMapRecordsByStreetcodeHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains(
                $"Cannot find streetcode with id: {streetcodeId}",
                result.Errors.First().Message);

            loggerMock.Verify(
                l => l.LogError(
                    query,
                    $"Cannot find streetcode with id: {streetcodeId}"),
                Times.Once);

            repositoryMock.Verify(
                r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(
                    It.IsAny<int>()),
                Times.Never);

            mapperMock.Verify(
                m => m.Map<IEnumerable<HistoryMapRecordDTO>>(
                    It.IsAny<object>()),
                Times.Never);
        }
    }
}