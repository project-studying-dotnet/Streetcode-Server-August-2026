using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Create;
using Streetcode.DAL.Entities.AdditionalContent.Coordinates.Types;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Coordinate
{
    public class CreateCoordinateHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CreateCoordinateHandler _handler;

        public CreateCoordinateHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _handler = new CreateCoordinateHandler(_mockRepositoryWrapper.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_ValidData_ReturnsSuccessResult()
        {
            var command = new CreateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns(new StreetcodeCoordinate());

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Create(It.IsAny<StreetcodeCoordinate>()));
            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
        }

        [Fact]
        public async Task Handle_MapReturnsNull_ReturnsFailResultWithCorrectMessage()
        {
            var command = new CreateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns((StreetcodeCoordinate)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Cannot convert null to streetcodeCoordinate", result.Errors[0].Message);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ReturnsFailResultWithCorrectMessage()
        {
            var command = new CreateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns(new StreetcodeCoordinate());

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Create(It.IsAny<StreetcodeCoordinate>()));

            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Failed to create a streetcodeCoordinate", result.Errors[0].Message);
        }
    }
}