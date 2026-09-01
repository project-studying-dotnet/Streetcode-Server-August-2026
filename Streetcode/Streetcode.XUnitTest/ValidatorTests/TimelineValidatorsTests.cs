// <copyright file="TimelineValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Timeline;
    using Streetcode.BLL.MediatR.Timeline.HistoricalContext.Validators;
    using Streetcode.BLL.MediatR.Timeline.TimelineItem.Create;
    using Streetcode.BLL.MediatR.Timeline.TimelineItem.Delete;
    using Streetcode.BLL.MediatR.Timeline.TimelineItem.Update;
    using Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;
    using Streetcode.DAL.Enums;
    using Xunit;
    using HistoricalContextEntity =
        Streetcode.DAL.Entities.Timeline.HistoricalContext;

    public class TimelineValidatorsTests
    {
        private readonly HistoricalContextDtoValidator _contextValidator = new ();
        private readonly TimelineItemCreateUpdateDtoValidator _timelineItemValidator;

        public TimelineValidatorsTests()
        {
            _timelineItemValidator = new TimelineItemCreateUpdateDtoValidator(
                _contextValidator);
        }

        [Theory]
        [InlineData("Історія України")]
        [InlineData("European History")]
        [InlineData("Культура")]
        public void ValidateContext_WhenNewTitleContainsLettersAndSpaces_ShouldBeValid(
            string title)
        {
            var context = new HistoricalContextDTO
            {
                Id = 0,
                Title = title,
            };

            var result = _contextValidator.Validate(context);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateContext_WhenIdIsNegative_ShouldBeInvalid()
        {
            var context = new HistoricalContextDTO
            {
                Id = -1,
                Title = "History",
            };

            var result = _contextValidator.Validate(context);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(HistoricalContextDTO.Id) &&
                    error.ErrorMessage ==
                    "Historical context ID cannot be negative.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("History 2026")]
        [InlineData("History!")]
        [InlineData("History-Code")]
        public void ValidateContext_WhenNewTitleIsInvalid_ShouldBeInvalid(
            string title)
        {
            var context = new HistoricalContextDTO
            {
                Id = 0,
                Title = title,
            };

            var result = _contextValidator.Validate(context);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(HistoricalContextDTO.Title));
        }

        [Fact]
        public void ValidateContext_WhenNewTitleIsTooLong_ShouldBeInvalid()
        {
            var context = new HistoricalContextDTO
            {
                Id = 0,
                Title = new string(
                    'A',
                    HistoricalContextEntity.TitleMaxLength + 1),
            };

            var result = _contextValidator.Validate(context);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(HistoricalContextDTO.Title) &&
                    error.ErrorMessage.Contains(
                        HistoricalContextEntity.TitleMaxLength.ToString(),
                        StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateContext_WhenExistingIdIsPositive_ShouldIgnoreSubmittedTitle()
        {
            var context = new HistoricalContextDTO
            {
                Id = 1,
                Title = string.Empty,
            };

            var result = _contextValidator.Validate(context);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateTimelineItem_WhenDataIsValid_ShouldBeValid()
        {
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.HistoricalContexts = new[]
            {
                new HistoricalContextDTO { Id = 1 },
                new HistoricalContextDTO { Title = "Culture" },
            };

            var result = _timelineItemValidator.Validate(timelineItem);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateTimelineItem_WhenRequiredValuesAreInvalid_ShouldBeInvalid()
        {
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.StreetcodeId = 0;
            timelineItem.Title = string.Empty;
            timelineItem.Description = string.Empty;
            timelineItem.Date = default;
            timelineItem.DateViewPattern = (DateViewPattern)999;

            var result = _timelineItemValidator.Validate(timelineItem);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.StreetcodeId));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.Title));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.Description));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.Date));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.DateViewPattern));
        }

        [Fact]
        public void ValidateTimelineItem_WhenTextFieldsAreTooLong_ShouldBeInvalid()
        {
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.Title = new string(
                'A',
                TimelineItemCreateUpdateDTO.TitleMaxLength + 1);
            timelineItem.Description = new string(
                'A',
                TimelineItemCreateUpdateDTO.DescriptionMaxLength + 1);

            var result = _timelineItemValidator.Validate(timelineItem);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.Title));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.Description));
        }

        [Fact]
        public void ValidateTimelineItem_WhenContextCollectionIsNull_ShouldBeInvalid()
        {
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.HistoricalContexts = null!;

            var result = _timelineItemValidator.Validate(timelineItem);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(TimelineItemCreateUpdateDTO.HistoricalContexts) &&
                    error.ErrorMessage ==
                    "Historical contexts collection is required.");
        }

        [Fact]
        public void ValidateTimelineItem_WhenNestedContextIsInvalid_ShouldIncludeNestedError()
        {
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.HistoricalContexts = new[]
            {
                new HistoricalContextDTO
                {
                    Id = 0,
                    Title = "Invalid 123",
                },
            };

            var result = _timelineItemValidator.Validate(timelineItem);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(HistoricalContextDTO.Title),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateCreate_WhenTimelineItemIsNull_ShouldBeInvalid()
        {
            var validator = new CreateTimelineItemCommandValidator(
                _timelineItemValidator);

            var result = validator.Validate(
                new CreateTimelineItemCommand(null!));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(CreateTimelineItemCommand.TimelineItem) &&
                    error.ErrorMessage == "Timeline item is required.");
        }

        [Fact]
        public void ValidateCreate_WhenNestedTimelineItemIsInvalid_ShouldIncludeNestedError()
        {
            var validator = new CreateTimelineItemCommandValidator(
                _timelineItemValidator);
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.Title = string.Empty;

            var result = validator.Validate(
                new CreateTimelineItemCommand(timelineItem));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(TimelineItemCreateUpdateDTO.Title),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenIdAndNestedTimelineItemAreInvalid_ShouldBeInvalid()
        {
            var validator = new UpdateTimelineItemCommandValidator(
                _timelineItemValidator);
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();
            timelineItem.Description = string.Empty;

            var result = validator.Validate(
                new UpdateTimelineItemCommand(0, timelineItem));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(UpdateTimelineItemCommand.Id));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(TimelineItemCreateUpdateDTO.Description),
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateDelete_WhenIdIsNotPositive_ShouldBeInvalid(int id)
        {
            var validator = new DeleteTimelineItemCommandValidator();

            var result = validator.Validate(
                new DeleteTimelineItemCommand(id));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    nameof(DeleteTimelineItemCommand.Id) &&
                    error.ErrorMessage ==
                    "Timeline item ID must be greater than 0.");
        }

        [Fact]
        public void ValidateCommands_WhenDataIsValid_ShouldBeValid()
        {
            var createValidator = new CreateTimelineItemCommandValidator(
                _timelineItemValidator);
            var updateValidator = new UpdateTimelineItemCommandValidator(
                _timelineItemValidator);
            var deleteValidator = new DeleteTimelineItemCommandValidator();
            TimelineItemCreateUpdateDTO timelineItem = CreateValidTimelineItem();

            var createResult = createValidator.Validate(
                new CreateTimelineItemCommand(timelineItem));
            var updateResult = updateValidator.Validate(
                new UpdateTimelineItemCommand(1, timelineItem));
            var deleteResult = deleteValidator.Validate(
                new DeleteTimelineItemCommand(1));

            Assert.True(createResult.IsValid);
            Assert.True(updateResult.IsValid);
            Assert.True(deleteResult.IsValid);
        }

        private static TimelineItemCreateUpdateDTO CreateValidTimelineItem()
        {
            return new TimelineItemCreateUpdateDTO
            {
                StreetcodeId = 1,
                Title = "Historical event",
                Description = "Event description",
                Date = new DateTime(1891, 1, 1),
                DateViewPattern = DateViewPattern.DateMonthYear,
                HistoricalContexts = Array.Empty<HistoricalContextDTO>(),
            };
        }
    }
}
