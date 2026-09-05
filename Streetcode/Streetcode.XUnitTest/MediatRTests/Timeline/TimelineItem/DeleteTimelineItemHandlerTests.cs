// <copyright file="DeleteTimelineItemHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Delete;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Moq;
    using Xunit;
    using TimelineItemEntity =
        global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class DeleteTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ITimelineRepository> timelineRepositoryMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();
        private readonly DeleteTimelineItemHandler handler;

        public DeleteTimelineItemHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(this.timelineRepositoryMock.Object);

            this.handler = new DeleteTimelineItemHandler(
                this.repositoryWrapperMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenTimelineItemDoesNotExist_ShouldReturnFailure()
        {
            const int timelineItemId = 99;
            var command = new DeleteTimelineItemCommand(timelineItemId);
            string expectedError =
                $"Cannot find a timeline item with corresponding id: {timelineItemId}";

            this.timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    null))
                .ReturnsAsync((TimelineItemEntity?)null);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.Delete(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            var timelineItem = new TimelineItemEntity { Id = 42 };
            var command = new DeleteTimelineItemCommand(timelineItem.Id);
            const string expectedError = "Failed to delete timeline item.";

            this.SetupExistingTimelineItem(timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.timelineRepositoryMock.Verify(
                repository => repository.Delete(timelineItem),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenTimelineItemExists_ShouldDeleteAndReturnSuccess()
        {
            var timelineItem = new TimelineItemEntity { Id = 42 };
            var command = new DeleteTimelineItemCommand(timelineItem.Id);

            this.SetupExistingTimelineItem(timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            this.timelineRepositoryMock.Verify(
                repository => repository.Delete(timelineItem),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(
                    It.IsAny<object>(),
                    It.IsAny<string>()),
                Times.Never());
        }

        private void SetupExistingTimelineItem(TimelineItemEntity timelineItem)
        {
            this.timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    null))
                .ReturnsAsync(timelineItem);
        }
    }
}
