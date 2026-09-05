// <copyright file="CreateTimelineItemHandlerTests.cs" company="PlaceholderCompany">
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
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;
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

    public class CreateTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();
        private readonly Mock<IStreetcodeRepository> streetcodeRepositoryMock = new ();
        private readonly Mock<ITimelineRepository> timelineRepositoryMock = new ();
        private readonly Mock<IHistoricalContextResolver> historicalContextResolverMock = new ();
        private readonly CreateTimelineItemHandler handler;

        public CreateTimelineItemHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.StreetcodeRepository)
                .Returns(this.streetcodeRepositoryMock.Object);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(this.timelineRepositoryMock.Object);

            this.handler = new CreateTimelineItemHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object,
                this.historicalContextResolverMock.Object);
        }

        [Fact]
        public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
        {
            const int streetcodeId = 999;
            var timelineItemDto = CreateTimelineItemDto(streetcodeId);
            var command = new CreateTimelineItemCommand(timelineItemDto);
            string expectedError =
                $"Cannot find a streetcode with corresponding id: {streetcodeId}";

            this.streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync((StreetcodeContent?)null);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDto>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenHistoricalContextDoesNotExist_ShouldReturnFailure()
        {
            const int missingContextId = 25;
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO { Id = missingContextId },
                });
            var timelineItemEntity = new TimelineItemEntity();
            var command = new CreateTimelineItemCommand(timelineItemDto);
            string expectedError =
                $"Cannot find historical contexts with IDs: {missingContextId}.";

            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            this.SetupContextResolutionFailure(expectedError);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.CreateAsync(It.IsAny<TimelineItemEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenHistoricalContextTitleAlreadyExists_ShouldReturnFailure()
        {
            const string contextTitle = "Culture";
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO { Title = contextTitle },
                });
            var timelineItemEntity = new TimelineItemEntity();
            var command = new CreateTimelineItemCommand(timelineItemDto);
            string expectedError =
                $"Historical contexts with titles already exist: {contextTitle}.";

            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            this.SetupContextResolutionFailure(expectedError);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.CreateAsync(It.IsAny<TimelineItemEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            var timelineItemDto = CreateTimelineItemDto();
            var timelineItemEntity = new TimelineItemEntity();
            var command = new CreateTimelineItemCommand(timelineItemDto);
            const string expectedError = "Failed to create timeline item.";

            this.SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            this.timelineRepositoryMock.Verify(
                repository => repository.CreateAsync(timelineItemEntity),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSaveChangesThrowsDbUpdateException_ShouldReturnFailure()
        {
            var timelineItemDto = CreateTimelineItemDto();
            var timelineItemEntity = new TimelineItemEntity();
            var command = new CreateTimelineItemCommand(timelineItemDto);
            var exception = new DbUpdateException("Database failure");
            const string expectedError = "Failed to create timeline item.";

            this.SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ThrowsAsync(exception);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            this.timelineRepositoryMock.Verify(
                repository => repository.CreateAsync(timelineItemEntity),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(command, exception.ToString()),
                Times.Once());
            this.timelineRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenCreatedItemCannotBeRetrieved_ShouldReturnFailure()
        {
            var timelineItemDto = CreateTimelineItemDto();
            var timelineItemEntity = new TimelineItemEntity { Id = 42 };
            var command = new CreateTimelineItemCommand(timelineItemDto);
            const string expectedError =
                "Created timeline item could not be retrieved.";

            this.SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            this.timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((TimelineItemEntity?)null);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(
                    It.IsAny<TimelineItemEntity>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenDataIsValid_ShouldCreateAndReturnTimelineItem()
        {
            const int existingContextId = 7;
            var timelineItemDto = CreateTimelineItemDto(
                historicalContexts: new[]
                {
                    new HistoricalContextDTO
                    {
                        Id = existingContextId,
                        Title = "Ignored title",
                    },
                    new HistoricalContextDTO
                    {
                        Id = existingContextId,
                        Title = "Duplicate ID",
                    },
                    new HistoricalContextDTO { Title = " Culture " },
                    new HistoricalContextDTO { Title = "culture" },
                });
            timelineItemDto.Title = " Test event ";
            timelineItemDto.Description = " Test description ";
            var timelineItemEntity = new TimelineItemEntity
            {
                Id = 42,
                Title = timelineItemDto.Title,
                Description = timelineItemDto.Description,
            };
            var savedTimelineItem = new TimelineItemEntity
            {
                Id = timelineItemEntity.Id,
                StreetcodeId = timelineItemDto.StreetcodeId,
                Title = "Test event",
                Description = "Test description",
            };
            var expectedDto = new TimelineItemDTO
            {
                Id = savedTimelineItem.Id,
                Title = savedTimelineItem.Title,
                Description = savedTimelineItem.Description,
            };
            var command = new CreateTimelineItemCommand(timelineItemDto);

            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            this.SetupContextResolutionSuccess(
                new[]
                {
                    new HistoricalContextTimelineEntity
                    {
                        HistoricalContextId = existingContextId,
                    },
                    new HistoricalContextTimelineEntity
                    {
                        HistoricalContext = new HistoricalContextEntity
                        {
                            Title = "Culture",
                        },
                    },
                });
            this.timelineRepositoryMock
                .Setup(repository => repository.CreateAsync(timelineItemEntity))
                .ReturnsAsync(timelineItemEntity);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            this.timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(savedTimelineItem);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(savedTimelineItem))
                .Returns(expectedDto);

            var result = await this.handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedDto, result.Value);
            Assert.Equal("Test event", timelineItemEntity.Title);
            Assert.Equal("Test description", timelineItemEntity.Description);
            Assert.Equal(2, timelineItemEntity.HistoricalContextTimelines.Count);
            Assert.Contains(
                timelineItemEntity.HistoricalContextTimelines,
                relation => relation.HistoricalContextId == existingContextId);
            Assert.Contains(
                timelineItemEntity.HistoricalContextTimelines,
                relation => relation.HistoricalContext?.Title == "Culture");
            this.timelineRepositoryMock.Verify(
                repository => repository.CreateAsync(timelineItemEntity),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(savedTimelineItem),
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
                Title = "Test event",
                Description = "Test description",
                Date = new DateTime(1891, 1, 1),
                HistoricalContexts = historicalContexts ??
                    Array.Empty<HistoricalContextDTO>(),
            };
        }

        private void SetupExistingStreetcode(int streetcodeId)
        {
            this.streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });
        }

        private void SetupCreationBeforeSave(
            TimelineItemCreateUpdateDto timelineItemDto,
            TimelineItemEntity timelineItemEntity)
        {
            this.SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            this.mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            this.SetupContextResolutionSuccess();
            this.timelineRepositoryMock
                .Setup(repository => repository.CreateAsync(timelineItemEntity))
                .ReturnsAsync(timelineItemEntity);
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
