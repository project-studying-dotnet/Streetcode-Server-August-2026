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
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetById;

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    public class GetTimelineItemByIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetTimelineItemByIdHandlerTests()
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
        public async Task Handle_WhenRepositoryReturnsTimelineItem_ShouldReturnSuccess()
        {
            TimelineItemEntity timelineItem = new TimelineItemEntity() { Id = 1};
            TimelineItemDTO expectedTimelineItem = new TimelineItemDTO() { Id = 1};

            _timelineRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItem);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(timelineItem))
                .Returns(expectedTimelineItem);

            GetTimelineItemByIdHandler handler = new GetTimelineItemByIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            GetTimelineItemByIdQuery query = new GetTimelineItemByIdQuery(1);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItem, result.Value);
            _timelineRepositoryMock.Verify(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<TimelineItemDTO>(timelineItem), Times.Once());
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemByIdQuery>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsNull_ShouldReturnFailure()
        {
            _timelineRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((TimelineItemEntity?)null);

            GetTimelineItemByIdHandler handler = new GetTimelineItemByIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            GetTimelineItemByIdQuery query = new GetTimelineItemByIdQuery(42);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal($"Cannot find a timeline item with corresponding id: {query.Id}", result.Errors[0].Message);
            _timelineRepositoryMock.Verify(repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            _mapperMock.Verify(mapper => mapper.Map<TimelineItemDTO>(It.IsAny<TimelineItemEntity>()), Times.Never());
            _loggerMock.Verify(logger => logger.LogError(query, $"Cannot find a timeline item with corresponding id: {query.Id}"), Times.Once());
        }
    }
}