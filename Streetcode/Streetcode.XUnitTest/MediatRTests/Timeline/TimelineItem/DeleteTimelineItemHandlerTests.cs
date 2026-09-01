// <copyright file="DeleteTimelineItemHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    using System.Linq.Expressions;
    using Moq;
    using Xunit;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Delete;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using TimelineItemEntity =
        global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class DeleteTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new ();
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock = new ();
        private readonly Mock<ILoggerService> _loggerMock = new ();
        private readonly DeleteTimelineItemHandler _handler;

        public DeleteTimelineItemHandlerTests()
        {
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(_timelineRepositoryMock.Object);

            _handler = new DeleteTimelineItemHandler(
                _repositoryWrapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenTimelineItemDoesNotExist_ShouldReturnFailure()
        {
            const int timelineItemId = 99;
            var command = new DeleteTimelineItemCommand(timelineItemId);
            string expectedError =
                $"Cannot find a timeline item with corresponding id: {timelineItemId}";

            _timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    null))
                .ReturnsAsync((TimelineItemEntity?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _timelineRepositoryMock.Verify(
                repository => repository.Delete(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            var timelineItem = new TimelineItemEntity { Id = 42 };
            var command = new DeleteTimelineItemCommand(timelineItem.Id);
            const string expectedError = "Failed to delete timeline item.";

            SetupExistingTimelineItem(timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _timelineRepositoryMock.Verify(
                repository => repository.Delete(timelineItem),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenTimelineItemExists_ShouldDeleteAndReturnSuccess()
        {
            var timelineItem = new TimelineItemEntity { Id = 42 };
            var command = new DeleteTimelineItemCommand(timelineItem.Id);

            SetupExistingTimelineItem(timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _timelineRepositoryMock.Verify(
                repository => repository.Delete(timelineItem),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(
                    It.IsAny<object>(),
                    It.IsAny<string>()),
                Times.Never());
        }

        private void SetupExistingTimelineItem(TimelineItemEntity timelineItem)
        {
            _timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    null))
                .ReturnsAsync(timelineItem);
        }
    }
}
