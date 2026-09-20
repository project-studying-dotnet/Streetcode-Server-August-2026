// <copyright file="GetAllTimelineItemsHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using AutoMapper;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.GetAll;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using TimelineItemDTO = global::Streetcode.BLL.DTO.Timeline.TimelineItemDTO;
    using TimelineItemEntity = global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class GetAllTimelineItemsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> timelineRepositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public GetAllTimelineItemsHandlerTests()
        {
            this.repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            this.timelineRepositoryMock = new Mock<ITimelineRepository>();
            this.mapperMock = new Mock<IMapper>();
            this.loggerMock = new Mock<ILoggerService>();
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(this.timelineRepositoryMock.Object);
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

            this.timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItems);
            this.mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems))
                .Returns(expectedTimelineItems);

            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(It.IsAny<GetAllTimelineItemsQuery>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsNull_ShouldReturnFailure()
        {
            this.timelineRepositoryMock
                .Setup(
                    repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((IEnumerable<TimelineItemEntity>)null!);
            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(TestMessages.CannotFindAnyTimelineItem, result.Errors.First().Message);
            this.loggerMock.Verify(logger => logger.LogError(query, TestMessages.CannotFindAnyTimelineItem), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(It.IsAny<object>()), Times.Never());
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
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

            this.timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItems);
            this.mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems))
                .Returns(expectedTimelineItems);

            GetAllTimelineItemsHandler handler = new GetAllTimelineItemsHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetAllTimelineItemsQuery query = new GetAllTimelineItemsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(It.IsAny<GetAllTimelineItemsQuery>(), It.IsAny<string>()), Times.Never());
        }
    }
}
