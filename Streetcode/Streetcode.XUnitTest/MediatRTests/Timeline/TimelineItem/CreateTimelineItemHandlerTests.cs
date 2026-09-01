// <copyright file="CreateTimelineItemHandlerTests.cs" company="PlaceholderCompany">
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
    using global::Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;
    using global::Streetcode.DAL.Entities.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using HistoricalContextEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContext;
    using TimelineItemEntity =
        global::Streetcode.DAL.Entities.Timeline.TimelineItem;

    public class CreateTimelineItemHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new ();
        private readonly Mock<IMapper> _mapperMock = new ();
        private readonly Mock<ILoggerService> _loggerMock = new ();
        private readonly Mock<IStreetcodeRepository> _streetcodeRepositoryMock = new ();
        private readonly Mock<IHistoricalContextRepository> _historicalContextRepositoryMock = new ();
        private readonly Mock<ITimelineRepository> _timelineRepositoryMock = new ();
        private readonly CreateTimelineItemHandler _handler;

        public CreateTimelineItemHandlerTests()
        {
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.StreetcodeRepository)
                .Returns(_streetcodeRepositoryMock.Object);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.HistoricalContextRepository)
                .Returns(_historicalContextRepositoryMock.Object);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.TimelineRepository)
                .Returns(_timelineRepositoryMock.Object);

            _handler = new CreateTimelineItemHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenStreetcodeDoesNotExist_ShouldReturnFailure()
        {
            const int streetcodeId = 999;
            var timelineItemDto = CreateTimelineItemDto(streetcodeId);
            var command = new CreateTimelineItemCommand(timelineItemDto);
            string expectedError =
                $"Cannot find a streetcode with corresponding id: {streetcodeId}";

            _streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync((StreetcodeContent?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemEntity>(
                    It.IsAny<TimelineItemCreateUpdateDTO>()),
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

            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            _historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _timelineRepositoryMock.Verify(
                repository => repository.Create(It.IsAny<TimelineItemEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
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

            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
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
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _timelineRepositoryMock.Verify(
                repository => repository.Create(It.IsAny<TimelineItemEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
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

            SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            _timelineRepositoryMock.Verify(
                repository => repository.Create(timelineItemEntity),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _timelineRepositoryMock.Verify(
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

            SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ThrowsAsync(exception);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
            _timelineRepositoryMock.Verify(
                repository => repository.Create(timelineItemEntity),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(command, exception.ToString()),
                Times.Once());
            _timelineRepositoryMock.Verify(
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

            SetupCreationBeforeSave(timelineItemDto, timelineItemEntity);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            _timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync((TimelineItemEntity?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal(expectedError, result.Errors[0].Message);
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedError),
                Times.Once());
            _mapperMock.Verify(
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

            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            _historicalContextRepositoryMock
                .SetupSequence(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(new[]
                {
                    new HistoricalContextEntity { Id = existingContextId },
                })
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());
            _timelineRepositoryMock
                .Setup(repository => repository.Create(timelineItemEntity))
                .Returns(timelineItemEntity);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            _timelineRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TimelineItemEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<TimelineItemEntity>,
                        IIncludableQueryable<TimelineItemEntity, object>>?>()))
                .ReturnsAsync(savedTimelineItem);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemDTO>(savedTimelineItem))
                .Returns(expectedDto);

            var result = await _handler.Handle(command, CancellationToken.None);

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
            _timelineRepositoryMock.Verify(
                repository => repository.Create(timelineItemEntity),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _mapperMock.Verify(
                mapper => mapper.Map<TimelineItemDTO>(savedTimelineItem),
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
                Title = "Test event",
                Description = "Test description",
                Date = new DateTime(1891, 1, 1),
                HistoricalContexts = historicalContexts ??
                    Array.Empty<HistoricalContextDTO>(),
            };
        }

        private void SetupExistingStreetcode(int streetcodeId)
        {
            _streetcodeRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    null))
                .ReturnsAsync(new StreetcodeContent { Id = streetcodeId });
        }

        private void SetupCreationBeforeSave(
            TimelineItemCreateUpdateDTO timelineItemDto,
            TimelineItemEntity timelineItemEntity)
        {
            SetupExistingStreetcode(timelineItemDto.StreetcodeId);
            _mapperMock
                .Setup(mapper => mapper.Map<TimelineItemEntity>(timelineItemDto))
                .Returns(timelineItemEntity);
            _historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    null))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());
            _timelineRepositoryMock
                .Setup(repository => repository.Create(timelineItemEntity))
                .Returns(timelineItemEntity);
        }
    }
}
