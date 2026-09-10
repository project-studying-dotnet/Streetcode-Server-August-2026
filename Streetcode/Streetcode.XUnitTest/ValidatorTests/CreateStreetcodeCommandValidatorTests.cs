// <copyright file="CreateStreetcodeCommandValidatorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using System;
    using System.Collections.Generic;
    using Streetcode.BLL.DTO.AdditionalContent.Tag;
    using Streetcode.BLL.DTO.Streetcode.Create;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;
    using Streetcode.DAL.Enums;
    using Xunit;

    public class CreateStreetcodeCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenValuesAreAtLimits_ShouldBeValid()
        {
            var validator = new CreateStreetcodeCommandValidator();
            var command = new CreateStreetcodeCommand(CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10000)]
        public void Validate_WhenIndexIsOutOfRange_ShouldBeInvalid(int index)
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();
            dto.Index = index;

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(CreateStreetcodeDTO.Index),
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(nameof(CreateStreetcodeDTO.Title), 101)]
        [InlineData(nameof(CreateStreetcodeDTO.FirstName), 51)]
        [InlineData(nameof(CreateStreetcodeDTO.LastName), 51)]
        [InlineData(nameof(CreateStreetcodeDTO.ShortDescription), 34)]
        [InlineData(nameof(CreateStreetcodeDTO.DateString), 51)]
        public void Validate_WhenTextExceedsLimit_ShouldBeInvalid(
            string propertyName,
            int valueLength)
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();

            var property = typeof(CreateStreetcodeDTO).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(dto, new string('a', valueLength));

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

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
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();
            dto.TransliterationUrl = url;

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(CreateStreetcodeDTO.TransliterationUrl),
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(nameof(CreateStreetcodeDTO.Title))]
        [InlineData(nameof(CreateStreetcodeDTO.DateString))]
        [InlineData(nameof(CreateStreetcodeDTO.TransliterationUrl))]
        public void Validate_WhenRequiredFieldIsEmpty_ShouldBeInvalid(string propertyName)
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();

            var property = typeof(CreateStreetcodeDTO).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(dto, string.Empty);

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    propertyName,
                    StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(nameof(CreateStreetcodeDTO.FirstName))]
        [InlineData(nameof(CreateStreetcodeDTO.LastName))]
        public void Validate_WhenPersonNameFieldIsEmpty_ShouldBeInvalid(string propertyName)
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();
            dto.StreetcodeType = StreetcodeType.Person;

            var property = typeof(CreateStreetcodeDTO).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(dto, string.Empty);

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    propertyName,
                    StringComparison.Ordinal));
        }

        [Fact]
        public void Validate_WhenEventTypeAndNameFieldsAreEmpty_ShouldBeValid()
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();
            dto.StreetcodeType = StreetcodeType.Event;
            dto.FirstName = null;
            dto.LastName = null;

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenTagTitleExceedsLimit_ShouldBeInvalid()
        {
            var validator = new CreateStreetcodeCommandValidator();
            CreateStreetcodeDTO dto = CreateValidDto();
            dto.Tags = new List<StreetcodeTagDTO>
            {
                new StreetcodeTagDTO { Id = 1, Title = new string('a', 51), IsVisible = true, Index = 0 },
            };

            var result = validator.Validate(new CreateStreetcodeCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(StreetcodeTagDTO.Title),
                    StringComparison.Ordinal));
        }

        private static CreateStreetcodeDTO CreateValidDto()
        {
            return new CreateStreetcodeDTO
            {
                Index = 1,
                Title = new string('a', 100),
                ShortDescription = new string('a', 33),
                StreetcodeType = StreetcodeType.Person,
                FirstName = new string('a', 50),
                LastName = new string('a', 50),
                EventStartOrPersonBirthDate = DateTime.UtcNow,
                DateString = "2026",
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
