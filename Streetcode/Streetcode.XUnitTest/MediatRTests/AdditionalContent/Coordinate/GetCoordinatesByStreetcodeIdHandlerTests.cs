using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.GetByStreetcodeId;
using Streetcode.DAL.Entities.AdditionalContent.Coordinates.Types;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Coordinate
{
    public class GetCoordinatesByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetCoordinatesByStreetcodeIdHandler _handler;

        public GetCoordinatesByStreetcodeIdHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetCoordinatesByStreetcodeIdHandler(
                _mockRepositoryWrapper.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsSuccessResult_WithCorrectTypeAndCount()
        {
            var query = new GetCoordinatesByStreetcodeIdQuery(1);
            var coordinatesList = new List<StreetcodeCoordinate>
            {
                new StreetcodeCoordinate { Id = 1, StreetcodeId = 1 },
                new StreetcodeCoordinate { Id = 2, StreetcodeId = 1 }
            };
            var dtoList = new List<StreetcodeCoordinateDTO>
            {
                new StreetcodeCoordinateDTO { Id = 1 },
                new StreetcodeCoordinateDTO { Id = 2 }
            };

            _mockRepositoryWrapper.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
                .ReturnsAsync(new StreetcodeContent { Id = 1 });

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.GetAllAsync(
                It.IsAny<Expression<Func<StreetcodeCoordinate, bool>>>(), null))
                .ReturnsAsync(coordinatesList);

            _mockMapper.Setup(m => m.Map<IEnumerable<StreetcodeCoordinateDTO>>(It.IsAny<IEnumerable<StreetcodeCoordinate>>()))
                .Returns(dtoList);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsAssignableFrom<IEnumerable<StreetcodeCoordinateDTO>>(result.Value);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task Handle_StreetcodeDoesNotExist_ReturnsFailResultWithCorrectMessage()
        {
            var query = new GetCoordinatesByStreetcodeIdQuery(99);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
                .ReturnsAsync((StreetcodeContent)null!);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal($"Cannot find a coordinates by a streetcode id: {query.StreetcodeId}, because such streetcode doesn`t exist", result.Errors[0].Message);
        }

        [Fact]
        public async Task Handle_CoordinatesReturnNull_LogsError_AndReturnsFailResult()
        {
            var query = new GetCoordinatesByStreetcodeIdQuery(1);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<StreetcodeContent, bool>>>(), null))
                .ReturnsAsync(new StreetcodeContent { Id = 1 });

            _mockRepositoryWrapper.Setup(r => r.StreetcodeCoordinateRepository.GetAllAsync(
                It.IsAny<Expression<Func<StreetcodeCoordinate, bool>>>(), null))
                .ReturnsAsync((IEnumerable<StreetcodeCoordinate>)null!);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal($"Cannot find a coordinates by a streetcode id: {query.StreetcodeId}", result.Errors[0].Message);

            _mockLogger.Verify(l => l.LogError(query, $"Cannot find a coordinates by a streetcode id: {query.StreetcodeId}"), Times.Once);
        }
    }
}