using System.Linq;
using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.Delete;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatR.HistoryMap.Delete
{
    public class DeleteHistoryMapRecordHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock;
        private readonly Mock<ILoggerService> loggerMock;

        public DeleteHistoryMapRecordHandlerTests()
        {
            repositoryMock = new Mock<IRepositoryWrapper>();
            loggerMock = new Mock<ILoggerService>();
        }

        [Fact]
        public async Task Handle_RecordExists_ShouldDeleteAndReturnOk()
        {
            // Arrange
            var command = new DeleteHistoryMapRecordCommand(1);
            var recordToDelete = new HistoryMapRecord { Id = 1 };

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync(recordToDelete);

            repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var handler = new DeleteHistoryMapRecordHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Delete(recordToDelete), Times.Once);
            repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_RecordNotFound_ShouldReturnFailResult()
        {
            // Arrange
            var command = new DeleteHistoryMapRecordCommand(99);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync((HistoryMapRecord)null!);

            var handler = new DeleteHistoryMapRecordHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find history map record with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Delete(It.IsAny<HistoryMapRecord>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DatabaseSaveFails_ShouldReturnFailResult()
        {
            // Arrange
            var command = new DeleteHistoryMapRecordCommand(1);
            var recordToDelete = new HistoryMapRecord { Id = 1 };

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync(recordToDelete);

            repositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            var handler = new DeleteHistoryMapRecordHandler(repositoryMock.Object, loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains("Failed to delete hisotry map record", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
        }
    }
}