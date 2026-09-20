using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.HistoryMap.Create;
using Streetcode.BLL.MediatR.HistoryMap.Merge;
using Streetcode.BLL.MediatR.HistoryMap.Validators;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap.Validators
{
    public class HistoryMapValidatorTests
    {
        [Fact]
        public void CreateDto_ValidData_ShouldBeValid()
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateDto_InvalidStreetcodeId_ShouldBeInvalid(int streetcodeId)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.StreetcodeId = streetcodeId;

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.StreetcodeId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateDto_InvalidToponymId_ShouldBeInvalid(int toponymId)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.ToponymId = toponymId;

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.ToponymId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateDto_InvalidPhysicalStreetcodeNumber_ShouldBeInvalid(int physicalStreetcodeNumber)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.PhysicalStreetcodeNumber = physicalStreetcodeNumber;

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.PhysicalStreetcodeNumber));
        }

        [Theory]
        [InlineData(-90d)]
        [InlineData(90d)]
        public void CreateDto_LatitudeOnBoundary_ShouldBeValid(double latitude)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.Latitude = (decimal)latitude;

            var result = validator.Validate(dto);

            Assert.DoesNotContain(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.Latitude));
        }

        [Theory]
        [InlineData(-90.01d)]
        [InlineData(90.01d)]
        [InlineData(-100d)]
        [InlineData(100d)]
        public void CreateDto_LatitudeOutOfRange_ShouldBeInvalid(double latitude)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.Latitude = (decimal)latitude;

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.Latitude));
        }

        [Theory]
        [InlineData(-180d)]
        [InlineData(180d)]
        public void CreateDto_LongitudeOnBoundary_ShouldBeValid(double longitude)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.Longitude = (decimal)longitude;

            var result = validator.Validate(dto);

            Assert.DoesNotContain(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.Longitude));
        }

        [Theory]
        [InlineData(-180.01d)]
        [InlineData(180.01d)]
        [InlineData(-200d)]
        [InlineData(200d)]
        public void CreateDto_LongitudeOutOfRange_ShouldBeInvalid(double longitude)
        {
            var validator = new CreateHistoryMapRecordDtoValidator();
            var dto = CreateValidDto();
            dto.Longitude = (decimal)longitude;

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordDTO.Longitude));
        }

        [Fact]
        public void CreateCommand_NullDto_ShouldBeInvalid()
        {
            // Arrange
            var validator = new CreateHistoryMapRecordCommandValidator(
                new CreateHistoryMapRecordDtoValidator());

            var command = new CreateHistoryMapRecordCommand(null!);

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateHistoryMapRecordCommand.Dto));
        }

        [Fact]
        public void CreateCommand_InvalidDto_ShouldBeInvalid()
        {
            // Arrange
            var validator = new CreateHistoryMapRecordCommandValidator(
                new CreateHistoryMapRecordDtoValidator());

            var dto = CreateValidDto();
            dto.StreetcodeId = 0;

            var command = new CreateHistoryMapRecordCommand(dto);

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                         nameof(CreateHistoryMapRecordCommand.Dto) + "." +
                         nameof(CreateHistoryMapRecordDTO.StreetcodeId));
        }

         [Fact]
        public void MergeDto_ValidData_ShouldBeValid()
        {
            // Arrange
            var validator = new MergeToponymsDtoValidator();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 1,
                TargetToponymId = 2
            };

            // Act
            var result = validator.Validate(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void MergeDto_InvalidSourceToponymId_ShouldBeInvalid(int sourceToponymId)
        {
            // Arrange
            var validator = new MergeToponymsDtoValidator();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = sourceToponymId,
                TargetToponymId = 2
            };

            // Act
            var result = validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                         nameof(MergeToponymsDTO.SourceToponymId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void MergeDto_InvalidTargetToponymId_ShouldBeInvalid(int targetToponymId)
        {
            // Arrange
            var validator = new MergeToponymsDtoValidator();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 1,
                TargetToponymId = targetToponymId
            };

            // Act
            var result = validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                         nameof(MergeToponymsDTO.TargetToponymId));
        }

        [Fact]
        public void MergeDto_SameSourceAndTarget_ShouldBeInvalid()
        {
            // Arrange
            var validator = new MergeToponymsDtoValidator();

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 5,
                TargetToponymId = 5
            };

            // Act
            var result = validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName == nameof(MergeToponymsDTO.SourceToponymId) &&
                    error.ErrorMessage == "Source and target toponyms cannot be the same.");
        }

        [Fact]
        public void CreateCommand_ValidDto_ShouldBeValid()
        {
            // Arrange
            var validator = new CreateHistoryMapRecordCommandValidator(
                new CreateHistoryMapRecordDtoValidator());

            var command = new CreateHistoryMapRecordCommand(CreateValidDto());

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void MergeCommand_NullDto_ShouldBeInvalid()
        {
            // Arrange
            var validator = new MergeToponymsCommandValidator(
                new MergeToponymsDtoValidator());

            var command = new MergeToponymsCommand(null!);

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(MergeToponymsCommand.Dto));
        }

        [Fact]
        public void MergeCommand_InvalidDto_ShouldBeInvalid()
        {
            // Arrange
            var validator = new MergeToponymsCommandValidator(
                new MergeToponymsDtoValidator());

            var dto = new MergeToponymsDTO
            {
                SourceToponymId = 5,
                TargetToponymId = 5
            };

            var command = new MergeToponymsCommand(dto);

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                         nameof(MergeToponymsCommand.Dto) + "." +
                         nameof(MergeToponymsDTO.SourceToponymId));
        }

        [Fact]
        public void MergeCommand_ValidDto_ShouldBeValid()
        {
            // Arrange
            var validator = new MergeToponymsCommandValidator(
                new MergeToponymsDtoValidator());

            var command = new MergeToponymsCommand(
                new MergeToponymsDTO
                {
                    SourceToponymId = 1,
                    TargetToponymId = 2
                });

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        private static CreateHistoryMapRecordDTO CreateValidDto()
        {
            return new CreateHistoryMapRecordDTO
            {
                StreetcodeId = 1,
                ToponymId = 2,
                PhysicalStreetcodeNumber = 1,
                Latitude = 0,
                Longitude = 0,
            };
        }
    }
}