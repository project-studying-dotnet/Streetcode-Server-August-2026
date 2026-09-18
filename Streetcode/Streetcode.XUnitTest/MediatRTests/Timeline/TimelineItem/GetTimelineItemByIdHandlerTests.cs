// <copyright file="GetTimelineItemByIdHandlerTests.cs" company="PlaceholderCompany">
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
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.GetById;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using TimelineItemDTO = global::Streetcode.BLL.DTO.Timeline.TimelineItemDTO;
    using TimelineItemEntity = global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class GetTimelineItemByIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> timelineRepositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public GetTimelineItemByIdHandlerTests()
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
        public async Task Handle_WhenRepositoryReturnsTimelineItem_ShouldReturnSuccess()
        {
            TimelineItemEntity timelineItem = new TimelineItemEntity() { Id = 1 };
            TimelineItemDTO expectedTimelineItem = new TimelineItemDTO() { Id = 1 };

            this.timelineRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItem);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(timelineItem))
                .Returns(expectedTimelineItem);

            GetTimelineItemByIdHandler handler = new GetTimelineItemByIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);

            GetTimelineItemByIdQuery query = new GetTimelineItemByIdQuery(1);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItem, result.Value);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<TimelineItemDTO>(timelineItem), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemByIdQuery>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsNull_ShouldReturnFailure()
        {
            this.timelineRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((TimelineItemEntity?)null);

            GetTimelineItemByIdHandler handler = new GetTimelineItemByIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);

            GetTimelineItemByIdQuery query = new GetTimelineItemByIdQuery(42);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal($"Cannot find a timeline item with corresponding id: {query.Id}", result.Errors[0].Message);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<TimelineItemDTO>(It.IsAny<TimelineItemEntity>()), Times.Never());
            this.loggerMock.Verify(logger => logger.LogError(query, $"Cannot find a timeline item with corresponding id: {query.Id}"), Times.Once());
        }

        [Fact]
        public async Task Handle_WhenLookingUpTimelineItem_ShouldUseRequestedIdPredicate()
        {
            const int requestedId = 42;
            var matchingTimelineItem = new TimelineItemEntity { Id = requestedId };
            var otherTimelineItem = new TimelineItemEntity { Id = requestedId + 1 };
            var expectedTimelineItem = new TimelineItemDTO { Id = requestedId };

            this.timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.Is<Expression<Func<TimelineItemEntity, bool>>>(predicate =>
                        predicate.Compile()(matchingTimelineItem) &&
                        !predicate.Compile()(otherTimelineItem)),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(matchingTimelineItem);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(matchingTimelineItem))
                .Returns(expectedTimelineItem);
            var handler = new GetTimelineItemByIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);

            var result = await handler.Handle(
                new GetTimelineItemByIdQuery(requestedId),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Same(expectedTimelineItem, result.Value);
        }
    }
}
