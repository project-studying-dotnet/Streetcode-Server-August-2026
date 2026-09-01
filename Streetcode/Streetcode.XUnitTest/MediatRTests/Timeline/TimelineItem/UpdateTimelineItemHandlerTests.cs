// <copyright file="UpdateTimelineItemHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    using System.Linq.Expressions;
    using AutoMapper;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using global::Streetcode.BLL.DTO.Timeline;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Update;
    using global::Streetcode.DAL.Entities.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using HistoricalContextEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContext;
    using HistoricalContextTimelineEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContextTimeline;
    using TimelineItemEntity =
        global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class UpdateTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new ();
        private readonly Mock<IMapper> _mapperMock = new ();
        private readonly Mock<ILoggerService> _loggerMock = new ();
        private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new ();
        private readonly Mock<IHistoricalContextRepository> _historicalContextRepositoryMock = new ();
        private readonly Mock<IHistoricalContextTimelineRepository> _relationRepositoryMock = new ();
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock = new ();
        private readonly UpdateTimelineItemHandler _handler;

        public UpdateTimelineItemHandlerTests()
        {
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.StreetcodeRepository)
                .Returns(_streetcodeRepositoryMock.Object);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.HistoricalContextRepository)
                .Returns(_historicalContextRepositoryMock.Object);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.HistoricalContextTimelineRepository)
                .Returns(_relationRepositoryMock.Object);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(_timelineRepositoryMock.Object);

            _handler = new UpdateTimelineItemHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenTimelineItemDoesNotExist_ShouldReturnFailure()
        {
            const int timelineItemId = 99;
            var command = new UpdateTimelineItemCommand(
                timelineItemId,
                CreateTimelineItemDto());
            string expectedError =
                $"Cannot find a timeline item with corresponding id: {timelineItemId}";

            SetupTimelineLookup(null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _streetcodeRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto(streetcodeId: 999);
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            string expectedError =
                $"Cannot find a streetcode with corresponding id: " +
                $"{timelineItemDto.StreetcodeId}";

            SetupTimelineLookup(timelineItem);
            _streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync((StreetcodeContent?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _historicalContextRepositoryMock.Verify(
                repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenHistoricalContextDoesNotExist_ShouldReturnFailure()
        {
            const int missingContextId = 25;
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO { Id = missingContextId },
                });
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            string expectedError =
                $"Cannot find historical contexts with IDs: {missingContextId}.";

            SetupTimelineLookup(timelineItem);
            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemCreateUpdateDTO, TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDTO>(),
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenHistoricalContextTitleAlreadyExists_ShouldReturnFailure()
        {
            const string contextTitle = "Culture";
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO { Title = contextTitle },
                });
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            string expectedError =
                $"Historical contexts with titles already exist: {contextTitle}.";

            SetupTimelineLookup(timelineItem);
            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _historicalContextRepositoryMock
                .SetupSequence(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>())
                .ReturnsAsync(new[]
                {
                    new HistoricalContextEntity { Id = 1, Title = contextTitle },
                });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemCreateUpdateDTO, TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDTO>(),
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto();
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            const string expectedError = "Failed to update timeline item.";

            SetupTimelineLookup(timelineItem);
            SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesThrowsDbUpdateException_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto();
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            var exception = new DbUpdateException("Database failure");
            const string expectedError = "Failed to update timeline item.";

            SetupTimelineLookup(timelineItem);
            SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ThrowsAsync(exception);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _timelineRepositoryMock.Verify(
                repository => repository.Update(timelineItem),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(command, exception.ToString()),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenUpdatedItemCannotBeRetrieved_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto();
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            const string expectedError =
                "Updated timeline item could not be retrieved.";

            SetupTimelineLookupSequence(timelineItem, null);
            SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenDataIsValid_ShouldSynchronizeContextsAndReturnSuccess()
        {
            const int removedContextId = 10;
            const int retainedContextId = 20;
            const int addedContextId = 30;
            var removedRelation = new HistoricalContextTimelineEntity
            {
                TimelineId = 42,
                HistoricalContextId = removedContextId,
            };
            var retainedRelation = new HistoricalContextTimelineEntity
            {
                TimelineId = 42,
                HistoricalContextId = retainedContextId,
            };
            var timelineItem = CreateTimelineItemEntity(
                historicalContextTimelines: new[]
                {
                    removedRelation,
                    retainedRelation,
                });
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO { Id = retainedContextId },
                    new HistoricalContextDTO { Id = retainedContextId },
                    new HistoricalContextDTO { Id = addedContextId },
                    new HistoricalContextDTO { Title = " Culture " },
                    new HistoricalContextDTO { Title = "culture" },
                });
            timelineItemDto.Title = " Updated event ";
            timelineItemDto.Description = " Updated description ";
            var expectedDto = new TimelineItemDTO
            {
                Id = timelineItem.Id,
                Title = "Updated event",
                Description = "Updated description",
                HistoricalContexts = Array.Empty<HistoricalContextDTO>(),
            };
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);

            SetupTimelineLookupSequence(timelineItem, timelineItem);
            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _historicalContextRepositoryMock
                .SetupSequence(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(new[]
                {
                    new HistoricalContextEntity { Id = retainedContextId },
                    new HistoricalContextEntity { Id = addedContextId },
                })
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());
            SetupMapping(timelineItemDto, timelineItem);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(timelineItem))
                .Returns(expectedDto);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Same(expectedDto, result.Value);
            Assert.Equal("Updated event", timelineItem.Title);
            Assert.Equal("Updated description", timelineItem.Description);
            Assert.Equal(3, timelineItem.HistoricalContextTimelines.Count);
            Assert.Contains(retainedRelation, timelineItem.HistoricalContextTimelines);
            Assert.Contains(
                timelineItem.HistoricalContextTimelines,
                relation => relation.HistoricalContextId == addedContextId);
            Assert.Contains(
                timelineItem.HistoricalContextTimelines,
                relation => relation.HistoricalContext?.Title == "Culture");
            Assert.DoesNotContain(
                timelineItem.HistoricalContextTimelines,
                relation => relation.HistoricalContextId == removedContextId);
            _relationRepositoryMock.Verify(
                repository => repository.Delete(removedRelation),
                Times.Once());
            _relationRepositoryMock.Verify(
                repository => repository.Delete(retainedRelation),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _timelineRepositoryMock.Verify(
                repository => repository.Update(timelineItem),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(timelineItem),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(
                    It.IsAny<object>(),
                    It.IsAny<string>()),
                Times.Never());
        }

        private static TimelineItemCreateUpdateDTO CreateTimelineItemDto(
            int streetcodeId = 1,
            IEnumerable<HistoricalContextDTO>? historicalContexts = null)
        {
            return new TimelineItemCreateUpdateDTO
            {
                StreetcodeId = streetcodeId,
                Title = "Updated event",
                Description = "Updated description",
                Date = new DateTime(1900, 1, 1),
                HistoricalContexts = historicalContexts ??
                    Array.Empty<HistoricalContextDTO>(),
            };
        }

        private static TimelineItemEntity CreateTimelineItemEntity(
            IEnumerable<HistoricalContextTimelineEntity>? historicalContextTimelines = null)
        {
            return new TimelineItemEntity
            {
                Id = 42,
                StreetcodeId = 1,
                Title = "Original event",
                Description = "Original description",
                HistoricalContextTimelines = historicalContextTimelines?.ToList() ??
                    new List<HistoricalContextTimelineEntity>(),
            };
        }

        private void SetupTimelineLookup(TimelineItemEntity? timelineItem)
        {
            _timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(timelineItem);
        }

        private void SetupTimelineLookupSequence(
            TimelineItemEntity firstResult,
            TimelineItemEntity? secondResult)
        {
            _timelineRepositoryMock
                .SetupSequence(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(firstResult)
                .ReturnsAsync(secondResult);
        }

        private void SetupExistingStreetcode(int streetcodeId)
        {
            _streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });
        }

        private void SetupMapping(
            TimelineItemCreateUpdateDTO timelineItemDto,
            TimelineItemEntity timelineItem)
        {
            _mapperMock
                .Setup(mapper => mapper.Map<
                    TimelineItemCreateUpdateDTO,
                    TimelineItemEntity>(timelineItemDto, timelineItem))
                .Callback<TimelineItemCreateUpdateDTO, TimelineItemEntity>(
                    (source, destination) =>
                    {
                        destination.StreetcodeId = source.StreetcodeId;
                        destination.Title = source.Title;
                        destination.Description = source.Description;
                        destination.Date = source.Date;
                        destination.DateViewPattern = source.DateViewPattern;
                    })
                .Returns(timelineItem);
        }

        private void SetupUpdateBeforeSave(
            TimelineItemCreateUpdateDTO timelineItemDto,
            TimelineItemEntity timelineItem)
        {
            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());
            SetupMapping(timelineItemDto, timelineItem);
        }
    }
}
