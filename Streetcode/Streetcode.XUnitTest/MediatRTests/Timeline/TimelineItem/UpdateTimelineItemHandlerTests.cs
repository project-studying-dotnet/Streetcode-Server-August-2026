// <copyright file="UpdateTimelineItemHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Timeline.TimelineItem
{
    using System.Linq.Expressions;
    using AutoMapper;
    using FluentResults;
    using global::Streetcode.BLL.DTO.Timeline;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.Interfaces.Timeline;
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Update;
    using global::Streetcode.DAL.Entities.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using HistoricalContextEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContext;
    using HistoricalContextTimelineEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContextTimeline;
    using TimelineItemEntity =
        global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class UpdateTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();
        private readonly Mock<IStreetcodeRepository> streetcodeRepositoryMock = new ();
        private readonly Mock<IHistoricalContextTimelineRepository> relationRepositoryMock = new ();
        private readonly Mock<ITimelineRepository> timelineRepositoryMock = new ();
        private readonly Mock<IHistoricalContextResolver> historicalContextResolverMock = new ();
        private readonly UpdateTimelineItemHandler handler;

        public UpdateTimelineItemHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.StreetcodeRepository)
                .Returns(this.streetcodeRepositoryMock.Object);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.HistoricalContextTimelineRepository)
                .Returns(this.relationRepositoryMock.Object);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(this.timelineRepositoryMock.Object);

            this.handler = new UpdateTimelineItemHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object,
                this.historicalContextResolverMock.Object);
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

            this.SetupTimelineLookup(null);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.streetcodeRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto(streetcodeId: 999);
            timelineItem.StreetcodeId = timelineItemDto.StreetcodeId;
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            string expectedError =
                $"Cannot find a streetcode with corresponding id: " +
                $"{timelineItemDto.StreetcodeId}";

            this.SetupTimelineLookup(timelineItem);
            this.streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync((StreetcodeContent?)null);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.historicalContextResolverMock.Verify(
                resolver => resolver.ResolveAsync(
                    It.IsAny<IEnumerable<HistoricalContextDTO>>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenStreetcodeIdChanges_ShouldReturnFailure()
        {
            var timelineItem = CreateTimelineItemEntity();
            var timelineItemDto = CreateTimelineItemDto(streetcodeId: 999);
            var command = new UpdateTimelineItemCommand(
                timelineItem.Id,
                timelineItemDto);
            string expectedError =
                $"Cannot move timeline item with id {timelineItem.Id} to another streetcode";
            this.SetupTimelineLookup(timelineItem);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(
                expectedError,
                Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(
                    command,
                    expectedError), Times.Once());
            this.streetcodeRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null), Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
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

            this.SetupTimelineLookup(timelineItem);
            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.SetupContextResolutionFailure(expectedError);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemCreateUpdateDto, TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDto>(),
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
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

            this.SetupTimelineLookup(timelineItem);
            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.SetupContextResolutionFailure(expectedError);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemCreateUpdateDto, TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDto>(),
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
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

            this.SetupTimelineLookup(timelineItem);
            this.SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.mapperMock.Verify(
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

            this.SetupTimelineLookup(timelineItem);
            this.SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ThrowsAsync(exception);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.timelineRepositoryMock.Verify(
                repository => repository.Update(timelineItem),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(command, exception.ToString()),
                Times.Once());
            this.mapperMock.Verify(
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

            this.SetupTimelineLookupSequence(timelineItem, null);
            this.SetupUpdateBeforeSave(timelineItemDto, timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.mapperMock.Verify(
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

            this.SetupTimelineLookupSequence(timelineItem, timelineItem);
            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.SetupContextResolutionSuccess(
                new[]
                {
                    new HistoricalContextTimelineEntity
                    {
                        HistoricalContextId = retainedContextId,
                    },
                    new HistoricalContextTimelineEntity
                    {
                        HistoricalContextId = addedContextId,
                    },
                    new HistoricalContextTimelineEntity
                    {
                        HistoricalContext = new HistoricalContextEntity
                        {
                            Title = "Culture",
                        },
                    },
                });
            this.SetupMapping(timelineItemDto, timelineItem);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(timelineItem))
                .Returns(expectedDto);

            var result = await this.handler.Handle(command, CancellationToken.None);

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
            this.relationRepositoryMock.Verify(
                repository => repository.Delete(removedRelation),
                Times.Once());
            this.relationRepositoryMock.Verify(
                repository => repository.Delete(retainedRelation),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.Update(timelineItem),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(timelineItem),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(
                    It.IsAny<object>(),
                    It.IsAny<string>()),
                Times.Never());
        }

        private static TimelineItemCreateUpdateDto CreateTimelineItemDto(
            int streetcodeId = 1,
            IEnumerable<HistoricalContextDTO>? historicalContexts = null)
        {
            return new TimelineItemCreateUpdateDto
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
            this.timelineRepositoryMock
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
            this.timelineRepositoryMock
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
            this.streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });
        }

        private void SetupMapping(
            TimelineItemCreateUpdateDto timelineItemDto,
            TimelineItemEntity timelineItem)
        {
            this.mapperMock
                .Setup(mapper => mapper.Map<
                    TimelineItemCreateUpdateDto,
                    TimelineItemEntity>(timelineItemDto, timelineItem))
                .Callback<TimelineItemCreateUpdateDto, TimelineItemEntity>(
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
            TimelineItemCreateUpdateDto timelineItemDto,
            TimelineItemEntity timelineItem)
        {
            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.SetupContextResolutionSuccess();
            this.SetupMapping(timelineItemDto, timelineItem);
        }

        private void SetupContextResolutionFailure(string errorMessage)
        {
            this.historicalContextResolverMock
                .Setup(resolver => resolver.ResolveAsync(
                    It.IsAny<IEnumerable<HistoricalContextDTO>>()))
                .ReturnsAsync(
                    Result.Fail<IReadOnlyCollection<HistoricalContextTimelineEntity>>(
                        errorMessage));
        }

        private void SetupContextResolutionSuccess(
            IReadOnlyCollection<HistoricalContextTimelineEntity>? contextRelations = null)
        {
            this.historicalContextResolverMock
                .Setup(resolver => resolver.ResolveAsync(
                    It.IsAny<IEnumerable<HistoricalContextDTO>>()))
                .ReturnsAsync(
                    Result.Ok<IReadOnlyCollection<HistoricalContextTimelineEntity>>(
                        contextRelations ?? Array.Empty<HistoricalContextTimelineEntity>()));
        }
    }
}
