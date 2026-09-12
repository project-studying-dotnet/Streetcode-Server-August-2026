// <copyright file="CoordinateValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.AdditionalContent.Coordinates;
    using Streetcode.BLL.DTO.AdditionalContent.Coordinates.Types;
    using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Create;
    using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Delete;
    using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.GetByStreetcodeId;
    using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Update;
    using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;
    using Xunit;

    public class CoordinateValidatorsTests
    {
        private readonly StreetcodeCoordinateDtoValidator _dtoValidator = new();

        [Fact]
        public void Validate_WhenCoordinatesAreOnBoundaries_ShouldBeValid()
        {
            var coordinates = new[]
            {
                CreateCoordinate(latitude: -90m, longitude: -180m),
                CreateCoordinate(latitude: 90m, longitude: 180m),
            };

            foreach (StreetcodeCoordinateDTO coordinate in coordinates)
            {
                var result = _dtoValidator.Validate(coordinate);

                Assert.True(result.IsValid);
            }
        }

        [Fact]
        public void Validate_WhenLatitudeIsOutOfRange_ShouldBeInvalid()
        {
            decimal[] invalidLatitudes = { -90.01m, 90.01m };

            foreach (decimal latitude in invalidLatitudes)
            {
                var result = _dtoValidator.Validate(
                    CreateCoordinate(latitude: latitude));

                Assert.Contains(
                    result.Errors,
                    error => error.PropertyName == nameof(CoordinateDTO.Latitude));
            }
        }

        [Fact]
        public void Validate_WhenLongitudeIsOutOfRange_ShouldBeInvalid()
        {
            decimal[] invalidLongitudes = { -180.01m, 180.01m };

            foreach (decimal longitude in invalidLongitudes)
            {
                var result = _dtoValidator.Validate(
                    CreateCoordinate(longitude: longitude));

                Assert.Contains(
                    result.Errors,
                    error => error.PropertyName == nameof(CoordinateDTO.Longtitude));
            }
        }

        [Fact]
        public void ValidateCreate_WhenCoordinateIsNull_ShouldBeInvalid()
        {
            var validator = new CreateCoordinateCommandValidator(_dtoValidator);

            var result = validator.Validate(new CreateCoordinateCommand(null!));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateCoordinateCommand.StreetcodeCoordinate));
        }

        [Fact]
        public void ValidateCreate_WhenNestedCoordinateIsInvalid_ShouldIncludeNestedErrors()
        {
            var validator = new CreateCoordinateCommandValidator(_dtoValidator);
            var command = new CreateCoordinateCommand(
                CreateCoordinate(streetcodeId: 0, latitude: 91m));

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(StreetcodeCoordinateDTO.StreetcodeId),
                    StringComparison.Ordinal));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(CoordinateDTO.Latitude),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenCoordinateIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new UpdateCoordinateCommandValidator(_dtoValidator);
            StreetcodeCoordinateDTO coordinate = CreateCoordinate();
            coordinate.Id = 0;

            var result = validator.Validate(
                new UpdateCoordinateCommand(coordinate));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(CoordinateDTO.Id),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateDeleteAndGet_WhenIdsAreNotPositive_ShouldBeInvalid()
        {
            var deleteValidator = new DeleteCoordinateCommandValidator();
            var getValidator = new GetCoordinatesByStreetcodeIdQueryValidator();

            var deleteResult = deleteValidator.Validate(
                new DeleteCoordinateCommand(0));
            var getResult = getValidator.Validate(
                new GetCoordinatesByStreetcodeIdQuery(-1));

            Assert.False(deleteResult.IsValid);
            Assert.False(getResult.IsValid);
        }

        private static StreetcodeCoordinateDTO CreateCoordinate(
            int streetcodeId = 1,
            decimal latitude = 0m,
            decimal longitude = 0m)
        {
            return new StreetcodeCoordinateDTO
            {
                Id = 1,
                StreetcodeId = streetcodeId,
                Latitude = latitude,
                Longtitude = longitude,
            };
        }
    }
}
