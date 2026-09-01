// <copyright file="UpdateStreetcodeCommandValidatorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using System;
    using System.Collections.Generic;
    using Streetcode.BLL.DTO.AdditionalContent.Tag;
    using Streetcode.BLL.DTO.Streetcode.Update;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Update;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;
    using Streetcode.DAL.Enums;
    using Xunit;

    public class UpdateStreetcodeCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenValuesAreAtLimits_ShouldBeValid()
        {
            var validator = new UpdateStreetcodeCommandValidator();
            var command = new UpdateStreetcodeCommand(1, CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new UpdateStreetcodeCommandValidator();
            var command = new UpdateStreetcodeCommand(0, CreateValidDto());

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(UpdateStreetcodeCommand.Id));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10000)]
        public void Validate_WhenIndexIsOutOfRange_ShouldBeInvalid(int index)
        {
            var validator = new UpdateStreetcodeCommandValidator();
            UpdateStreetcodeDTO dto = CreateValidDto();
            dto.Index = index;

            var result = validator.Validate(new UpdateStreetcodeCommand(1, dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(UpdateStreetcodeDTO.Index),
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(nameof(UpdateStreetcodeDTO.Title), 101)]
        [InlineData(nameof(UpdateStreetcodeDTO.FirstName), 51)]
        [InlineData(nameof(UpdateStreetcodeDTO.LastName), 51)]
        [InlineData(nameof(UpdateStreetcodeDTO.Teaser), 34)]
        public void Validate_WhenTextExceedsLimit_ShouldBeInvalid(
            string propertyName,
            int valueLength)
        {
            var validator = new UpdateStreetcodeCommandValidator();
            UpdateStreetcodeDTO dto = CreateValidDto();

            var property = typeof(UpdateStreetcodeDTO).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(dto, new string('a', valueLength));

            var result = validator.Validate(new UpdateStreetcodeCommand(1, dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    propertyName,
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("Has Spaces")]
        [InlineData("Has_Underscore")]
        [InlineData("UPPERCASE")]
        public void Validate_WhenTransliterationUrlHasInvalidCharacters_ShouldBeInvalid(string url)
        {
            var validator = new UpdateStreetcodeCommandValidator();
            UpdateStreetcodeDTO dto = CreateValidDto();
            dto.TransliterationUrl = url;

            var result = validator.Validate(new UpdateStreetcodeCommand(1, dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(UpdateStreetcodeDTO.TransliterationUrl),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void Validate_WhenTagTitleExceedsLimit_ShouldBeInvalid()
        {
            var validator = new UpdateStreetcodeCommandValidator();
            UpdateStreetcodeDTO dto = CreateValidDto();
            dto.Tags = new List<StreetcodeTagDTO>
            {
                new StreetcodeTagDTO { Id = 1, Title = new string('a', 51), IsVisible = true, Index = 0 },
            };

            var result = validator.Validate(new UpdateStreetcodeCommand(1, dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(StreetcodeTagDTO.Title),
                    StringComparison.Ordinal));
        }

        private static UpdateStreetcodeDTO CreateValidDto()
        {
            return new UpdateStreetcodeDTO
            {
                Id = 1,
                Index = 1,
                Title = new string('a', 100),
                StreetcodeType = StreetcodeType.Person,
                FirstName = new string('a', 50),
                LastName = new string('a', 50),
                EventStartOrPersonBirthDate = DateTime.UtcNow,
                DateString = "2020",
                Teaser = new string('a', 33),
                TransliterationUrl = "valid-url-123",
                Tags = new List<StreetcodeTagDTO>
                {
                    new StreetcodeTagDTO { Id = 1, Title = new string('a', 50), IsVisible = true, Index = 0 },
                },
            };
        }
    }
}
