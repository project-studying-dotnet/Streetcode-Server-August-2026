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
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetAll;

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    public class GetAllTimelineItemsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetAllTimelineItemsHandlerTests()
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
        public async Task Handle_WhenTimelineItemsExist_ShouldReturnSuccess()
        {
            List<TimelineItemEntity> timelineItems = new List<TimelineItemEntity>
            {
                new TimelineItemEntity(),
                new TimelineItemEntity(),
            };
            List<TimelineItemDTO> expectedTimelineItems = new List<TimelineItemDTO>
            {
                new TimelineItemDTO(),
                new TimelineItemDTO(),
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

            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            _timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<GetAllTimelineItemsQuery>(), It.IsAny<string>()), Times.Never());
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
            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal("Cannot find any timelineItem", result.Errors.First().Message);
            _loggerMock.Verify(logger => logger.LogError(query, "Cannot find any timelineItem"), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(It.IsAny<object>()), Times.Never());
            _timelineRepositoryMock.Verify(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsEmptyCollection_ShouldReturnSuccess()
        {
            List<TimelineItemEntity> timelineItems = new List<TimelineItemEntity>();
            List<TimelineItemDTO> expectedTimelineItems = new List<TimelineItemDTO>();

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

            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);
            
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
            _timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<GetAllTimelineItemsQuery>(), It.IsAny<string>()), Times.Never());
        }
    }
}
