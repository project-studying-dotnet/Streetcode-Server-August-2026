namespace Streetcode.XUnitTest.ValidatorTests
{
    using System;
    using Streetcode.BLL.DTO.Media.Art;
    using Streetcode.BLL.MediatR.Media.Art.Create;
    using Streetcode.BLL.MediatR.Media.Art.Delete;
    using Streetcode.BLL.MediatR.Media.Art.Update;
    using Streetcode.BLL.MediatR.Media.Art.Validators;
    using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;
    using Xunit;

    public class ArtValidatorsTests
    {
        [Fact]
        public void ValidateDto_WhenValid_ShouldBeValid()
        {
            var validator = new ArtUpdateCreateDtoValidator();

            var result = validator.Validate(CreateValidDto());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDto_WhenValuesAreAtLimits_ShouldBeValid()
        {
            var validator = new ArtUpdateCreateDtoValidator();
            var dto = CreateValidDto();
            dto.Title = new string('a', ArtEntity.TitleMaxLength);
            dto.Description = new string('a', ArtEntity.DescriptionMaxLength);

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDto_WhenTitleAndDescriptionAreNull_ShouldBeValid()
        {
            var validator = new ArtUpdateCreateDtoValidator();
            var dto = CreateValidDto();
            dto.Title = null;
            dto.Description = null;

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDto_WhenTitleExceedsMaxLength_ShouldBeInvalid()
        {
            var validator = new ArtUpdateCreateDtoValidator();
            var dto = CreateValidDto();
            dto.Title = new string('a', ArtEntity.TitleMaxLength + 1);

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(ArtUpdateCreateDto.Title));
        }

        [Fact]
        public void ValidateDto_WhenDescriptionExceedsMaxLength_ShouldBeInvalid()
        {
            var validator = new ArtUpdateCreateDtoValidator();
            var dto = CreateValidDto();
            dto.Description = new string('a', ArtEntity.DescriptionMaxLength + 1);

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(ArtUpdateCreateDto.Description));
        }

        [Fact]
        public void ValidateDto_WhenImageIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new ArtUpdateCreateDtoValidator();
            var dto = CreateValidDto();
            dto.ImageId = 0;

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(ArtUpdateCreateDto.ImageId));
        }

        [Fact]
        public void ValidateCreate_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new CreateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var command = new CreateArtCommand(CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCreate_WhenArtIsNull_ShouldBeInvalid()
        {
            var validator = new CreateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var command = new CreateArtCommand(null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(CreateArtCommand.Art));
        }

        [Fact]
        public void ValidateCreate_WhenArtIsInvalid_ShouldIncludeNestedErrors()
        {
            var validator = new CreateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var dto = CreateValidDto();
            dto.ImageId = 0;

            var result = validator.Validate(new CreateArtCommand(dto));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(ArtUpdateCreateDto.ImageId),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new UpdateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var command = new UpdateArtCommand(1, CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateUpdate_WhenIdAndArtAreInvalid_ShouldIncludeErrors()
        {
            var validator = new UpdateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var dto = CreateValidDto();
            dto.ImageId = 0;
            var command = new UpdateArtCommand(0, dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(UpdateArtCommand.Id));
            Assert.Contains(
                result.Errors,
                error => error.PropertyName.EndsWith(
                    nameof(ArtUpdateCreateDto.ImageId),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateUpdate_WhenArtIsNull_ShouldBeInvalid()
        {
            var validator = new UpdateArtCommandValidator(
                new ArtUpdateCreateDtoValidator());
            var command = new UpdateArtCommand(1, null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(UpdateArtCommand.Art));
        }

        [Fact]
        public void ValidateDelete_WhenIdIsValid_ShouldBeValid()
        {
            var validator = new DeleteArtCommandValidator();
            var command = new DeleteArtCommand(1);

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDelete_WhenIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new DeleteArtCommandValidator();
            var command = new DeleteArtCommand(0);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(DeleteArtCommand.Id));
        }

        private static ArtUpdateCreateDto CreateValidDto()
        {
            return new ArtUpdateCreateDto
            {
                ImageId = 1,
                Title = "Valid title",
                Description = "Valid description",
            };
        }
    }
}
