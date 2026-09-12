using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Delete;
using Streetcode.DAL.Entities.AdditionalContent.Coordinates.Types;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Coordinate
{
    public class DeleteCoordinateHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly DeleteCoordinateHandler _handler;

        public DeleteCoordinateHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _handler = new DeleteCoordinateHandler(_mockRepositoryWrapper.Object);
        }

        [Fact]
        public async Task Handle_CoordinateExists_ReturnsSuccessResult()
        {
            var command = new DeleteCoordinateCommand(1);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeCoordinate, bool>>>(), null))
                .ReturnsAsync(new StreetcodeCoordinate { Id = 1 });

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Delete(It.IsAny<StreetcodeCoordinate>()));
            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
        }

        [Fact]
        public async Task Handle_CoordinateNotFound_ReturnsFailResultWithCorrectMessage()
        {
            var command = new DeleteCoordinateCommand(99);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeCoordinate, bool>>>(), null))
                .ReturnsAsync((StreetcodeCoordinate)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(string.Format(TestMessages.CannotFindCoordinateWithCorrespondingCategoryId, command.Id), result.Errors[0].Message);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ReturnsFailResultWithCorrectMessage()
        {
            var command = new DeleteCoordinateCommand(1);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeCoordinate, bool>>>(), null))
                .ReturnsAsync(new StreetcodeCoordinate { Id = 1 });

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.Delete(It.IsAny<StreetcodeCoordinate>()));
            _mockRepositoryWrapper.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(TestMessages.FailedToDeleteCoordinate, result.Errors[0].Message);
        }
    }
}
