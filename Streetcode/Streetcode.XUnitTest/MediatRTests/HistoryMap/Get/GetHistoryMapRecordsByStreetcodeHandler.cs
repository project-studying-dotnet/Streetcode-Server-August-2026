using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.HistoryMap.GetByStreetcode
{
    public class GetHistoryMapRecordsByStreetcodeHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public GetHistoryMapRecordsByStreetcodeHandlerTests()
        {
            repositoryMock = new Mock<IRepositoryWrapper>();
            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILoggerService>();
        }

        [Fact]
        public async Task Handle_StreetcodeExists_ShouldReturnMappedRecords()
        {
            // Arrange
            int streetcodeId = 1;
            var query = new GetHistoryMapRecordsByStreetcodeQuery(streetcodeId);

            var dbRecords = new List<HistoryMapRecord>
            {
                new HistoryMapRecord { Id = 1, StreetcodeId = streetcodeId },
                new HistoryMapRecord { Id = 2, StreetcodeId = streetcodeId },
            };

            var expectedDtos = new List<HistoryMapRecordDTO>
            {
                new HistoryMapRecordDTO { Id = 1, StreetcodeId = streetcodeId },
                new HistoryMapRecordDTO { Id = 2, StreetcodeId = streetcodeId },
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>,
                        IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(streetcodeId))
                .ReturnsAsync(dbRecords);

            mapperMock.Setup(m => m.Map<IEnumerable<HistoryMapRecordDTO>>(dbRecords))
                .Returns(expectedDtos);

            var handler = new GetHistoryMapRecordsByStreetcodeHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count());
            Assert.Equal(expectedDtos, result.Value);
        }

        [Fact]
        public async Task Handle_StreetcodeNotFound_ShouldReturnFailResult()
        {
            // Arrange
            var query = new GetHistoryMapRecordsByStreetcodeQuery(99);

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>,
                        IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync((StreetcodeContent)null!);

            var handler = new GetHistoryMapRecordsByStreetcodeHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find streetcode with id: 99", result.Errors.First().Message);
            loggerMock.Verify(l => l.LogError(query, It.IsAny<string>()), Times.Once);
            repositoryMock.Verify(r => r.HistoryMapRecordRepository.GetByStreetcodeIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}