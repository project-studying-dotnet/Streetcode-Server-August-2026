// <copyright file="GetTimelineItemsByStreetcodeIdHandlerTests.cs" company="PlaceholderCompany">
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
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.GetByStreetcodeId;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using TimelineItemDTO = global::Streetcode.BLL.DTO.Timeline.TimelineItemDTO;
    using TimelineItemEntity = global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class GetTimelineItemsByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ITimelineRepository> timelineRepositoryMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<ILoggerService> loggerMock;

        public GetTimelineItemsByStreetcodeIdHandlerTests()
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
            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            var nonMatchingTimelineItem = new TimelineItemEntity
                {
                    StreetcodeId = 99,
                };
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                    It.Is<Expression<Func<TimelineItemEntity, bool>>>(
                        predicate =>
                            predicate.Compile()(timelineItems[0])
                            && !predicate.Compile()(nonMatchingTimelineItem)),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()),
                Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(timelineItems), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemsByStreetcodeIdQuery>(), It.IsAny<string>()), Times.Never());
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

            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal("Cannot find any timeline item by the streetcode id: 12", result.Errors.First().Message);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(query, "Cannot find any timeline item by the streetcode id: 12"), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(It.IsAny<IEnumerable<TimelineItemEntity>>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsEmptyCollection_ShouldReturnSuccess()
        {
            List<TimelineItemEntity> emptyTimelineItems = new List<TimelineItemEntity>();
            List<TimelineItemDTO> expectedTimelineItems = new List<TimelineItemDTO>();
            this.timelineRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(emptyTimelineItems);
            this.mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(emptyTimelineItems))
                .Returns(expectedTimelineItems);
            GetTimelineItemsByStreetcodeIdHandler handler = new GetTimelineItemsByStreetcodeIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
            GetTimelineItemsByStreetcodeIdQuery query = new GetTimelineItemsByStreetcodeIdQuery(12);
            var result = await handler.Handle(query, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedTimelineItems, result.Value);
            this.timelineRepositoryMock.Verify(
                repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                It.IsAny<Func<IQueryable<TimelineItemEntity>, IIncludableQueryable<TimelineItemEntity, object>>?>()), Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<IEnumerable<TimelineItemDTO>>(emptyTimelineItems), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(It.IsAny<GetTimelineItemsByStreetcodeIdQuery>(), It.IsAny<string>()), Times.Never());
        }
    }
}
