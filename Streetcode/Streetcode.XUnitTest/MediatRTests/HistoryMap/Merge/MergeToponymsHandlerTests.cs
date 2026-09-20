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

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap.Merge
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
            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 1,
                TargetToponymId = 2,
            };

            var command = new MergeToponymsCommand(dto);

            var sourceToponym = new Toponym
            {
                Id = dto.SourceToponymId,
            };

            var targetToponym = new Toponym
            {
                Id = dto.TargetToponymId,
            };

            var recordsToUpdate = new List<HistoryMapRecord>
            {
                new HistoryMapRecord
                {
                    Id = 1,
                    ToponymId = dto.SourceToponymId,
                },
                new HistoryMapRecord
                {
                    Id = 2,
                    ToponymId = dto.SourceToponymId,
                },
            };

            var sourceStreetcodeLinkToTransfer = new StreetcodeToponym
            {
                StreetcodeId = 10,
                ToponymId = dto.SourceToponymId,
            };

            var sourceStreetcodeLinkWithExistingTarget = new StreetcodeToponym
            {
                StreetcodeId = 20,
                ToponymId = dto.SourceToponymId,
            };

            var existingTargetStreetcodeLink = new StreetcodeToponym
            {
                StreetcodeId = 20,
                ToponymId = dto.TargetToponymId,
            };

            repositoryMock.SetupSequence(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(sourceToponym)
                .ReturnsAsync(targetToponym);

            repositoryMock.Setup(r =>
                    r.HistoryMapRecordRepository.GetByToponymIdAsync(dto.SourceToponymId))
                .ReturnsAsync(recordsToUpdate);

            repositoryMock.SetupSequence(r =>
                    r.StreetcodeToponymRepository.GetAllAsync(
                        It.IsAny<Expression<Func<StreetcodeToponym, bool>>>(),
                        It.IsAny<Func<IQueryable<StreetcodeToponym>, IIncludableQueryable<StreetcodeToponym, object>>>()))
                .ReturnsAsync(new List<StreetcodeToponym>
                {
                    sourceStreetcodeLinkToTransfer,
                    sourceStreetcodeLinkWithExistingTarget,
                })
                .ReturnsAsync(new List<StreetcodeToponym>
                {
                    existingTargetStreetcodeLink,
                });

            repositoryMock.Setup(r =>
                    r.StreetcodeToponymRepository.CreateAsync(It.IsAny<StreetcodeToponym>()))
                .ReturnsAsync((StreetcodeToponym link) => link);

            repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new MergeToponymsHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);

            Assert.All(
                recordsToUpdate,
                record => Assert.Equal(dto.TargetToponymId, record.ToponymId));

            repositoryMock.Verify(
                r => r.HistoryMapRecordRepository.Update(It.IsAny<HistoryMapRecord>()),
                Times.Exactly(recordsToUpdate.Count));

            repositoryMock.Verify(
                r => r.StreetcodeToponymRepository.Delete(
                    It.Is<StreetcodeToponym>(x =>
                        x.StreetcodeId == sourceStreetcodeLinkToTransfer.StreetcodeId &&
                        x.ToponymId == dto.SourceToponymId)),
                Times.Once);

            repositoryMock.Verify(
                r => r.StreetcodeToponymRepository.Delete(
                    It.Is<StreetcodeToponym>(x =>
                        x.StreetcodeId == sourceStreetcodeLinkWithExistingTarget.StreetcodeId &&
                        x.ToponymId == dto.SourceToponymId)),
                Times.Once);

            repositoryMock.Verify(
                r => r.StreetcodeToponymRepository.CreateAsync(
                    It.Is<StreetcodeToponym>(x =>
                        x.StreetcodeId == sourceStreetcodeLinkToTransfer.StreetcodeId &&
                        x.ToponymId == dto.TargetToponymId)),
                Times.Once);

            repositoryMock.Verify(
                r => r.StreetcodeToponymRepository.CreateAsync(
                    It.Is<StreetcodeToponym>(x =>
                        x.StreetcodeId == sourceStreetcodeLinkWithExistingTarget.StreetcodeId &&
                        x.ToponymId == dto.TargetToponymId)),
                Times.Never);

            repositoryMock.Verify(
                r => r.ToponymRepository.Delete(sourceToponym),
                Times.Once);

            repositoryMock.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_SourceToponymNotFound_ShouldReturnFailResult()
        {
            var command = new MergeToponymsCommand(new MergeToponymsDTO { SourceToponymId = 99, TargetToponymId = 2 });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync((Toponym)null!);

            var handler = new MergeToponymsHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find source toponym with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TargetToponymNotFound_ShouldReturnFailResult()
        {
            var command = new MergeToponymsCommand(new MergeToponymsDTO { SourceToponymId = 1, TargetToponymId = 99 });

            repositoryMock.SetupSequence(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = 1 })
                .ReturnsAsync((Toponym)null!);

            var handler = new MergeToponymsHandler(
                repositoryMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find target toponym with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);
        }
    }
}