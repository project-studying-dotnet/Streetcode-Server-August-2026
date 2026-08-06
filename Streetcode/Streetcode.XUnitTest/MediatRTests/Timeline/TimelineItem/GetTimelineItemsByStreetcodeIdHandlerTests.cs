using AutoMapper;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using TimelineItemEntity = Streetcode.DAL.Entities.Timeline.TimelineItem;
using TimelineItemDTO = Streetcode.BLL.DTO.Timeline.TimelineItemDTO;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetByStreetcodeId;

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    public class GetTimelineItemsByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetTimelineItemsByStreetcodeIdHandlerTests()
        {
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _timelineRepositoryMock = new Mock<ITimelineRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILoggerService>();

            _repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(_timelineRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsTimelineItemsByStreetcodeId_ShouldReturnSuccess()
        {
            List<TimelineItemEntity> timelineItems = new List<TimelineItemEntity>
            {
                new TimelineItemEntity() { Id = 1, StreetcodeId = 12 },
                new TimelineItemEntity() { Id = 2, StreetcodeId = 12 },
            };
            List<TimelineItemDTO> expectedTimelineItems = new List<TimelineItemDTO>
            {
                new TimelineItemDTO() { Id = 1 },
                new TimelineItemDTO() { Id = 2 },
            };
            _timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItems);
            _mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems))
                .Returns(expectedTimelineItems);
            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            var nonMatchingTimelineItem = new TimelineItemEntity
                {
                    StreetcodeId = 99,
                };
            _timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.Is<Expression<Func<TimelineItemEntity, bool>>>(
                        predicate =>
                            predicate.Compile()(timelineItems[0])
                            && !predicate.Compile()(nonMatchingTimelineItem)),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()),
                Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemsByStreetcodeIdQuery>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsNull_ShouldReturnFailure()
        {
            _timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((IEnumerable<TimelineItemEntity>?)null);
            
            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal("Cannot find any timeline item by the streetcode id: 12", result.Errors.First().Message);
            _timelineRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(query, "Cannot find any timeline item by the streetcode id: 12"), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(It.IsAny<IEnumerable<TimelineItemEntity>>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsEmptyCollection_ShouldReturnSuccess()
        {
            List<TimelineItemEntity> emptyTimelineItems = new List<TimelineItemEntity>();
            List<TimelineItemDTO> expectedTimelineItems = new List<TimelineItemDTO>();
            _timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(emptyTimelineItems);
            _mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(emptyTimelineItems))
                .Returns(expectedTimelineItems);
            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            _timelineRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(emptyTimelineItems), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemsByStreetcodeIdQuery>(), It.IsAny<string>()), Times.Never());
        }
    }
}