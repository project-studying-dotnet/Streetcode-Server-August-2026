// <copyright file="FactValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using System;
    using System.Collections.Generic;
    using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using Streetcode.BLL.MediatR.Streetcode.Fact.Create;
    using Streetcode.BLL.MediatR.Streetcode.Fact.Delete;
    using Streetcode.BLL.MediatR.Streetcode.Fact.Reorder;
    using Streetcode.BLL.MediatR.Streetcode.Fact.Update;
    using Streetcode.BLL.MediatR.Streetcode.Fact.Validators;
    using Xunit;

    public class FactValidatorsTests
    {
        [Fact]
        public void ValidateUpdateCreate_WhenValuesAreAtLimits_ShouldBeValid()
        {
            var validator = new FactUpdateCreateDtoValidator();
            FactUpdateCreateDto dto = CreateValidDto();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(nameof(FactUpdateCreateDto.Title), 69)]
        [InlineData(nameof(FactUpdateCreateDto.FactContent), 601)]
        [InlineData(nameof(FactUpdateCreateDto.ImageAlt), 201)]
        public void ValidateUpdateCreate_WhenTextExceedsLimit_ShouldBeInvalid(
            string propertyName,
            int valueLength)
        {
            var validator = new FactUpdateCreateDtoValidator();
            FactUpdateCreateDto dto = CreateValidDto();

            var property = typeof(FactUpdateCreateDto).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(dto, new string('a', valueLength));

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == propertyName);
        }

        [Fact]
        public void ValidateUpdateCreate_WhenRequiredValuesAreInvalid_ShouldBeInvalid()
        {
            var validator = new FactUpdateCreateDtoValidator();
            var dto = new FactUpdateCreateDto
            {
                Title = string.Empty,
                FactContent = string.Empty,
                ImageId = 0,
                StreetcodeId = 0,
            };

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(FactUpdateCreateDto.Title));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(FactUpdateCreateDto.FactContent));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(FactUpdateCreateDto.ImageId));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(FactUpdateCreateDto.StreetcodeId));
        }

        [Fact]
        public void ValidateUpdateCreate_WhenImageAltIsNull_ShouldBeValid()
        {
            var validator = new FactUpdateCreateDtoValidator();
            FactUpdateCreateDto dto = CreateValidDto();
            dto.ImageAlt = null;

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateReorder_WhenOrderIsEmpty_ShouldBeValid()
        {
            var validator = new FactReorderDtoValidator();
            var dto = new FactReorderDto
            {
                StreetcodeId = 1,
                OrderedFactIds = new List<int>(),
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateReorder_WhenOrderIsNull_ShouldBeInvalid()
        {
            var validator = new FactReorderDtoValidator();
            var dto = new FactReorderDto
            {
                StreetcodeId = 1,
                OrderedFactIds = null!,
            };

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(FactReorderDto.OrderedFactIds));
        }

        [Fact]
        public void ValidateReorder_WhenIdsAreNotPositive_ShouldBeInvalid()
        {
            var validator = new FactReorderDtoValidator();
            var dto = new FactReorderDto
            {
                StreetcodeId = 0,
                OrderedFactIds = new List<int> { 1, 0, -1 },
            };

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(FactReorderDto.StreetcodeId));
            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName.StartsWith(
                        nameof(FactReorderDto.OrderedFactIds)));
        }

        [Fact]
        public void ValidateReorder_WhenOrderContainsDuplicates_ShouldBeInvalid()
        {
            var validator = new FactReorderDtoValidator();
            var dto = new FactReorderDto
            {
                StreetcodeId = 1,
                OrderedFactIds = new List<int> { 1, 2, 2 },
            };

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(FactReorderDto.OrderedFactIds));
        }

        [Fact]
        public void ValidateCreate_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new CreateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            var command = new CreateFactCommand(CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCreate_WhenFactIsNull_ShouldBeInvalid()
        {
            var validator = new CreateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            var command = new CreateFactCommand(null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(CreateFactCommand.Fact));
        }

        [Fact]
        public void ValidateCreate_WhenFactIsInvalid_ShouldIncludeNestedErrors()
        {
            var validator = new CreateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            FactUpdateCreateDto fact = CreateValidDto();
            fact.Title = string.Empty;

            var result = validator.Validate(
                new CreateFactCommand(fact));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(FactUpdateCreateDto.Title),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new UpdateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            var command = new UpdateFactCommand(1, CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateUpdate_WhenIdAndFactAreInvalid_ShouldIncludeErrors()
        {
            var validator = new UpdateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            FactUpdateCreateDto fact = CreateValidDto();
            fact.FactContent = string.Empty;
            var command = new UpdateFactCommand(0, fact);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(UpdateFactCommand.Id));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(FactUpdateCreateDto.FactContent),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenFactIsNull_ShouldBeInvalid()
        {
            var validator = new UpdateFactCommandValidator(
                new FactUpdateCreateDtoValidator());
            var command = new UpdateFactCommand(1, null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(UpdateFactCommand.Fact));
        }

        [Fact]
        public void ValidateDelete_WhenIdIsPositive_ShouldBeValid()
        {
            var validator = new DeleteFactCommandValidator();

            var result = validator.Validate(
                new DeleteFactCommand(1));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDelete_WhenIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new DeleteFactCommandValidator();

            var result = validator.Validate(
                new DeleteFactCommand(0));

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(DeleteFactCommand.Id));
        }

        [Fact]
        public void ValidateReorderCommand_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new ReorderFactsCommandValidator(
                new FactReorderDtoValidator());
            var reorder = new FactReorderDto
            {
                StreetcodeId = 1,
                OrderedFactIds = new List<int>(),
            };

            var result = validator.Validate(
                new ReorderFactsCommand(reorder));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateReorderCommand_WhenReorderIsNull_ShouldBeInvalid()
        {
            var validator = new ReorderFactsCommandValidator(
                new FactReorderDtoValidator());
            var command = new ReorderFactsCommand(null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error =>
                    error.PropertyName ==
                    nameof(ReorderFactsCommand.Reorder));
        }

        [Fact]
        public void ValidateReorderCommand_WhenReorderIsInvalid_ShouldIncludeNestedErrors()
        {
            var validator = new ReorderFactsCommandValidator(
                new FactReorderDtoValidator());
            var reorder = new FactReorderDto
            {
                StreetcodeId = 0,
                OrderedFactIds = new List<int>(),
            };

            var result = validator.Validate(
                new ReorderFactsCommand(reorder));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(FactReorderDto.StreetcodeId),
                    StringComparison.Ordinal));
        }

        private static FactUpdateCreateDto CreateValidDto()
        {
            return new FactUpdateCreateDto
            {
                Title = new string('a', 68),
                FactContent = new string('a', 600),
                ImageAlt = new string('a', 200),
                ImageId = 1,
                StreetcodeId = 1,
            };
        }
    }
}
