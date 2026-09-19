using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.HistoryMap.Create;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap.Create
{
    public class CreateHistoryMapRecordHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public CreateHistoryMapRecordHandlerTests()
        {
            repositoryMock = new Mock<IRepositoryWrapper>();
            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILoggerService>();
        }

        [Fact]
        public async Task Handle_ValidData_ShoudlReturnOkResult()
        {
            var dto = new CreateHistoryMapRecordDTO { StreetcodeId = 1, ToponymId = 2, PhysicalStreetcodeNumber = 99 };
            var command = new CreateHistoryMapRecordCommand(dto);

            var mappedEntity = new HistoryMapRecord { Id = 1, StreetcodeId = 1, ToponymId = 2 };
            var resultDto = new HistoryMapRecordDTO { Id = 1, StreetcodeId = 1, ToponymId = 2 };

            SetupRepositoryMock(streetcodeExists: true, toponymExists: true, recordExists: false, saveSuccess: true);

            mapperMock.Setup(m => m.Map<HistoryMapRecord>(It.IsAny<CreateHistoryMapRecordDTO>()))
                .Returns(mappedEntity);
            mapperMock.Setup(m => m.Map<HistoryMapRecordDTO>(It.IsAny<HistoryMapRecord>()))
                .Returns(resultDto);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(resultDto.Id, result.Value.Id);
            Assert.Equal(resultDto.StreetcodeId, result.Value.StreetcodeId);
            Assert.Equal(resultDto.ToponymId, result.Value.ToponymId);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity), Times.Once);
            repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_StreetcodeNotFound_ShouldReturnFailResult()
        {
            var command = new CreateHistoryMapRecordCommand(new CreateHistoryMapRecordDTO { StreetcodeId = 99 });
            SetupRepositoryMock(streetcodeExists: false, toponymExists: true, recordExists: false, saveSuccess: true);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("Cannot find streetcode with id: 99", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);

        }

        [Fact]
        public async Task Handle_RecordAlreadyExists_ShouldReturnfailResult()
        {
            var command = new CreateHistoryMapRecordCommand(new CreateHistoryMapRecordDTO { StreetcodeId = 1, PhysicalStreetcodeNumber = 3 });

            SetupRepositoryMock(streetcodeExists: true, toponymExists: true, recordExists: true, saveSuccess: true);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("already exists for this streetcode", result.Errors.First().Message);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.CreateAsync(It.IsAny<HistoryMapRecord>()), Times.Never);
        }

        private void SetupRepositoryMock(bool streetcodeExists, bool toponymExists, bool recordExists, bool saveSuccess)
        {
            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(streetcodeExists ? new StreetcodeContent() : null);

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(toponymExists ? new Toponym() : null);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(recordExists ? new HistoryMapRecord() : null);

            repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(saveSuccess ? 1 : 0);
        }
    }
}