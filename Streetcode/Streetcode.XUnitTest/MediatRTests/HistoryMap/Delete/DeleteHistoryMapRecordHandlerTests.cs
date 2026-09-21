using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.Delete;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap.Delete
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
            const int recordId = 1;

            var command = new DeleteHistoryMapRecordCommand(recordId);

            var recordToDelete = new HistoryMapRecord { Id = recordId };

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync(recordToDelete);

            repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new DeleteHistoryMapRecordHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Delete(recordToDelete), Times.Once);
            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            loggerMock.Verify(l => l.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RecordNotFound_ShouldReturnFailResult()
        {
            const int recordId = 99;

            var command = new DeleteHistoryMapRecordCommand(recordId);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync((HistoryMapRecord)null!);

            var handler = new DeleteHistoryMapRecordHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains($"Cannot find history map record with id: {recordId}", result.Errors.First().Message);

            loggerMock.Verify(
                l => l.LogError(
                    command,
                    $"Cannot find history map record with id: {recordId}"),
                Times.Once);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Delete(It.IsAny<HistoryMapRecord>()), Times.Never);
            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DatabaseSaveFails_ShouldReturnFailResult()
        {
            const int recordId = 1;

            var command = new DeleteHistoryMapRecordCommand(recordId);

            var recordToDelete = new HistoryMapRecord { Id = recordId };

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<HistoryMapRecord, bool>>>(),
                    It.IsAny<Func<IQueryable<HistoryMapRecord>, IIncludableQueryable<HistoryMapRecord, object>>>()))
                .ReturnsAsync(recordToDelete);

            repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var handler = new DeleteHistoryMapRecordHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains($"Failed to delete history map record with id: {recordId}", result.Errors.First().Message);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.Delete(recordToDelete), Times.Once);

            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            loggerMock.Verify(l => l.LogError(command, $"Failed to delete history map record with id: {recordId}"), Times.Once);
        }
    }
}