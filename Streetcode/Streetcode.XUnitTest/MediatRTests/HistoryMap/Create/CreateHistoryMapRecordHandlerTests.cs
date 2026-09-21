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
            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 99,
                Latitude = 49.84m,
                Longitude = 24.03m,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            var mappedEntity = new HistoryMapRecord
            {
                Id = 1,
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
            };

            var resultDto = new HistoryMapRecordDTO
            {
                Id = 1,
                StreetcodeId = dto.StreetcodeId,
                ToponymId = dto.ToponymId,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = dto.StreetcodeId });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = dto.ToponymId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    dto.StreetcodeId, dto.PhysicalStreetcodeNumber))
                .ReturnsAsync((HistoryMapRecord)null!);

            mapperMock.Setup(m => m.Map<HistoryMapRecord>(dto))
                .Returns(mappedEntity);
            mapperMock.Setup(m => m.Map<HistoryMapRecordDTO>(mappedEntity))
                .Returns(resultDto);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity))
                .ReturnsAsync(mappedEntity);

            repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

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
            Assert.Equal(
                resultDto.PhysicalStreetcodeNumber,
                result.Value.PhysicalStreetcodeNumber);
            Assert.Equal(resultDto.Latitude, result.Value.Latitude);
            Assert.Equal(resultDto.Longitude, result.Value.Longitude);

            Assert.NotEqual(default, mappedEntity.CreatedAt);
            Assert.NotEqual(default, mappedEntity.UpdatedAt);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity), Times.Once);
            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            loggerMock.Verify(l => l.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StreetcodeNotFound_ShouldReturnFailResult()
        {
            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 99,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 1,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            repositoryMock.Setup(r =>
                    r.StreetcodeRepository.GetFirstOrDefaultAsync(
                        It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                        It.IsAny<Func<IQueryable<StreetcodeContent>,
                            IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync((StreetcodeContent)null!);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains(
                "Cannot find streetcode with id: 99",
                result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, It.IsAny<string>()), Times.Once);

            repositoryMock.Verify(
                r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>,
                        IIncludableQueryable<Toponym, object>>>()),
                Times.Never);

            repositoryMock.Verify(
                r => r.HistoryMapRecordRepository.CreateAsync(
                    It.IsAny<HistoryMapRecord>()),
                Times.Never);

            repositoryMock.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ToponymNotFound_ShouldReturnFailResult()
        {
            // Arrange
            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 99,
                PhysicalStreetcodeNumber = 1,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            repositoryMock.Setup(r =>
                    r.StreetcodeRepository.GetFirstOrDefaultAsync(
                        It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                        It.IsAny<Func<IQueryable<StreetcodeContent>,
                            IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent
                {
                    Id = dto.StreetcodeId,
                });

            repositoryMock.Setup(r =>
                    r.ToponymRepository.GetFirstOrDefaultAsync(
                        It.IsAny<Expression<Func<Toponym, bool>>>(),
                        It.IsAny<Func<IQueryable<Toponym>,
                            IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync((Toponym)null!);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            // Act
            var result = await handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);

            Assert.Contains(
                result.Errors,
                error => error.Message ==
                    "Cannot find toponym with id: 99");

            loggerMock.Verify(
                l => l.LogError(
                    command,
                    "Cannot find toponym with id: 99"),
                Times.Once);

            repositoryMock.Verify(
                r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);

            repositoryMock.Verify(
                r => r.HistoryMapRecordRepository.CreateAsync(
                    It.IsAny<HistoryMapRecord>()),
                Times.Never);

            repositoryMock.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RecordAlreadyExists_ShouldReturnFailResult()
        {
            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 3,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            var existingRecord = new HistoryMapRecord
            {
                Id = 10,
                StreetcodeId = dto.StreetcodeId,
                ToponymId = dto.ToponymId,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = dto.StreetcodeId });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = dto.ToponymId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    dto.StreetcodeId, dto.PhysicalStreetcodeNumber))
                .ReturnsAsync(existingRecord);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("already exists for this streetcode", result.Errors.First().Message);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.CreateAsync(It.IsAny<HistoryMapRecord>()), Times.Never);

            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            mapperMock.Verify(m => m.Map<HistoryMapRecord>(It.IsAny<CreateHistoryMapRecordDTO>()), Times.Never);

            loggerMock.Verify(
                l => l.LogError(
                    command,
                    It.Is<string>(message => message.Contains("already exists"))),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DatabaseSaveFails_ShouldRetunrFailResult()
        {
            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 5,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            var mappedEntity = new HistoryMapRecord
            {
                Id = 1,
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = dto.StreetcodeId });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = dto.ToponymId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    dto.StreetcodeId, dto.PhysicalStreetcodeNumber))
                .ReturnsAsync((HistoryMapRecord)null!);

            mapperMock.Setup(m => m.Map<HistoryMapRecord>(dto))
                .Returns(mappedEntity);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity))
                .ReturnsAsync(mappedEntity);

            repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Contains("Failed to create history map record.", result.Errors.First().Message);

            loggerMock.Verify(l => l.LogError(command, "Failed to create history map record."), Times.Once);

            repositoryMock.Verify(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity), Times.Once);

            repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            mapperMock.Verify(m => m.Map<HistoryMapRecordDTO>(It.IsAny<HistoryMapRecord>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldPassCancellationTokenToSaveChangesAsync()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 5,
            };

            var command = new CreateHistoryMapRecordCommand(dto);

            var mappedEntity = new HistoryMapRecord
            {
                Id = 1,
                StreetcodeId = dto.StreetcodeId,
                ToponymId = dto.ToponymId,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
            };

            repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(new StreetcodeContent { Id = dto.StreetcodeId });

            repositoryMock.Setup(r => r.ToponymRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>>()))
                .ReturnsAsync(new Toponym { Id = dto.ToponymId });

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.GetByStreetcodeAndNumberAsync(
                    dto.StreetcodeId, dto.PhysicalStreetcodeNumber))
                .ReturnsAsync((HistoryMapRecord)null!);

            mapperMock.Setup(m => m.Map<HistoryMapRecord>(dto))
                .Returns(mappedEntity);

            repositoryMock.Setup(r => r.HistoryMapRecordRepository.CreateAsync(mappedEntity))
                .ReturnsAsync(mappedEntity);

            repositoryMock.Setup(r => r.SaveChangesAsync(cancellationToken))
                .ReturnsAsync(1);

            var handler = new CreateHistoryMapRecordHandler(
                repositoryMock.Object,
                mapperMock.Object,
                loggerMock.Object);

            var result = await handler.Handle(command, cancellationToken);

            Assert.True(result.IsSuccess);

            repositoryMock.Verify(
                r => r.SaveChangesAsync(cancellationToken), Times.Once);
        }
    }
}