using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.Merge;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.HistoryMap.Merge
{
    public class MergeToponymsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock;
        private readonly Mock<ILoggerService> loggerMock;

        public MergeToponymsHandlerTests()
        {
            repositoryMock = new Mock<IRepositoryWrapper>();
            loggerMock = new Mock<ILoggerService>();
        }

        [Fact]
        public async Task Handle_ValidData_ShouldMergeToponymsAndReturnOk()
        {
            // Arrange
            var dto = new MergeToponymsDTO { SourceToponymId = 1, TargetToponymId = 2 };
            var command = new MergeToponymsCommand(dto);

            var sourceToponym = new Toponym { Id = 1 };
            var targetToponym = new Toponym { Id = 2 };
            var recordsToUpdate = new List<HistoryMapRecord>
            {
                new HistoryMapRecord { Id = 1, ToponymId = 1 },
                new HistoryMapRecord { Id = 2, ToponymId = 1 }
            };

            repositoryMock.SetupSequence(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(sourceToponym)
                .ReturnsAsync(targetToponym);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByToponymIdAsync(dto.SourceToponymId))
                .ReturnsAsync(recordsToUpdate);

            repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var handler = new MergeToponymsHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);

            Assert.All(recordsToUpdate, record => Assert.Equal(dto.TargetToponymId, record.ToponymId));

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Update(It.IsAny<HistoryMapRecord>()), Times.Exactly(2));
            repositoryMock.Verify(r => r.ToponymRepository.Delete(sourceToponym), Times.Once);
        }

        [Fact]
        public async Task Handle_SourceToponymNotFound_ShouldReturnFailResult()
        {
            // Arrange
            var command = new MergeToponymsCommand(new MergeToponymsDTO { SourceToponymId = 99, TargetToponymId = 2 });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync((Toponym)null!);

            var handler = new MergeToponymsHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find source toponym with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TargetToponymNotFound_ShouldReturnFailResult()
        {
            // Arrange
            var command = new MergeToponymsCommand(new MergeToponymsDTO { SourceToponymId = 1, TargetToponymId = 99 });

            repositoryMock.SetupSequence(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = 1 })
                .ReturnsAsync((Toponym)null!);

            var handler = new MergeToponymsHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find target toponym with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
        }
    }
}