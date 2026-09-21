using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.HistoryMap.Create;
using Streetcode.BLL.MediatR.HistoryMap.Delete;
using Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode;
using Streetcode.BLL.MediatR.HistoryMap.Merge;
using Streetcode.WebApi.Controllers.HistoryMap;
using Xunit;

namespace Streetcode.XUnitTest.Controllers
{
    public class HistoryMapControllerTests
    {
        [Fact]
        public async Task GetByStreetcodeId_ValidRequest_ShouldReturnOk()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            var expectedRecords = new List<HistoryMapRecordDTO>
            {
                new HistoryMapRecordDTO
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 20,
                    ToponymName = "Main Street",
                    PhysicalStreetcodeNumber = 1,
                    Latitude = 49.84m,
                    Longitude = 24.03m
                },
                new HistoryMapRecordDTO
                {
                    Id = 2,
                    StreetcodeId = 10,
                    ToponymId = 21,
                    ToponymName = "Second Street",
                    PhysicalStreetcodeNumber = 2,
                    Latitude = 49.85m,
                    Longitude = 24.04m
                }
            };

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<GetHistoryMapRecordsByStreetcodeQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Ok<IEnumerable<HistoryMapRecordDTO>>(expectedRecords));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.GetByStreetcodeId(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Same(expectedRecords, okResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<GetHistoryMapRecordsByStreetcodeQuery>(
                        query => query.StreetcodeId == 10),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByStreetcodeId_MediatorReturnsError_ShouldReturnBadRequest()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<GetHistoryMapRecordsByStreetcodeQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Fail<IEnumerable<HistoryMapRecordDTO>>("Streetcode not found"));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.GetByStreetcodeId(999);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequestResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<GetHistoryMapRecordsByStreetcodeQuery>(
                        query => query.StreetcodeId == 999),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Create_ValidRequest_ShouldReturnOk()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 10,
                ToponymId = 20,
                PhysicalStreetcodeNumber = 5,
                Latitude = 49.84m,
                Longitude = 24.03m
            };

            var expectedRecord = new HistoryMapRecordDTO
            {
                Id = 100,
                StreetcodeId = dto.StreetcodeId,
                ToponymId = dto.ToponymId,
                PhysicalStreetcodeNumber = dto.PhysicalStreetcodeNumber,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ToponymName = "Main Street"
            };

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<CreateHistoryMapRecordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(expectedRecord));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Same(expectedRecord, okResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<CreateHistoryMapRecordCommand>(
                        command => command.Dto == dto),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Create_MediatorReturnsError_ShouldReturnBadRequest()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            var dto = new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 10,
                ToponymId = 20,
                PhysicalStreetcodeNumber = 5,
                Latitude = 49.84m,
                Longitude = 24.03m
            };

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<CreateHistoryMapRecordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Fail<HistoryMapRecordDTO>(
                        "History map record already exists"));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.Create(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequestResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<CreateHistoryMapRecordCommand>(
                        command => command.Dto == dto),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Delete_ValidRequest_ShouldReturnOk()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<DeleteHistoryMapRecordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(Unit.Value));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.Delete(15);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(Unit.Value, okResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<DeleteHistoryMapRecordCommand>(
                        command => command.Id == 15),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Delete_MediatorReturnsError_ShouldReturnBadRequest()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<DeleteHistoryMapRecordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Fail<Unit>("History map record not found"));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.Delete(999);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequestResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<DeleteHistoryMapRecordCommand>(
                        command => command.Id == 999),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task MergeToponyms_ValidRequest_ShouldReturnOk()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 10,
                TargetToponymId = 20
            };

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<MergeToponymsCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(Unit.Value));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.MergeToponyms(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(Unit.Value, okResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<MergeToponymsCommand>(
                        command => command.Dto == dto),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task MergeToponyms_MediatorReturnsError_ShouldReturnBadRequest()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 10,
                TargetToponymId = 20
            };

            mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<MergeToponymsCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Fail<Unit>("Source and target toponyms cannot be merged"));

            var controller = CreateController(mediatorMock);

            // Act
            var result = await controller.MergeToponyms(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequestResult.Value);

            mediatorMock.Verify(
                m => m.Send(
                    It.Is<MergeToponymsCommand>(
                        command => command.Dto == dto),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static HistoryMapController CreateController(
            Mock<IMediator> mediatorMock)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IMediator>(mediatorMock.Object)
                .BuildServiceProvider();

            return new HistoryMapController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider
                    }
                }
            };
        }
    }
}
