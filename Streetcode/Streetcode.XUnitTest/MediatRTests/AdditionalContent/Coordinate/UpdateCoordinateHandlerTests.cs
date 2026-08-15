using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Update;
using Streetcode.DAL.Entities.AdditionalContent.Coordinates.Types;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Coordinate
{
    public class UpdateCoordinateHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UpdateCoordinateHandler _handler;

        public UpdateCoordinateHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _handler = new UpdateCoordinateHandler(_mockRepositoryWrapper.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_ValidData_ReturnsSuccessResult()
        {
            var command = new UpdateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns(new StreetcodeCoordinate());

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Update(It.IsAny<StreetcodeCoordinate>()));
            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
        }

        [Fact]
        public async Task Handle_MapReturnsNull_ReturnsFailResultWithCorrectMessage()
        {
            var command = new UpdateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns((StreetcodeCoordinate)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Cannot convert null to streetcodeCoordinate", result.Errors[0].Message);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ReturnsFailResultWithCorrectMessage()
        {
            var command = new UpdateCoordinateCommand(new StreetcodeCoordinateDTO());

            _mockMapper.Setup(m => m.Map<StreetcodeCoordinate>(It.IsAny<object>()))
                .Returns(new StreetcodeCoordinate());

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Update(It.IsAny<StreetcodeCoordinate>()));

            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Failed to update a streetcodeCoordinate", result.Errors[0].Message);
        }
    }
}