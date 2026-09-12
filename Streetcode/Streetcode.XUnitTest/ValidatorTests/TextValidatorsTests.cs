namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
    using Streetcode.BLL.MediatR.Streetcode.Text.Create;
    using Streetcode.BLL.MediatR.Streetcode.Text.Validators;
    using TextEntity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;
    using Xunit;

    public class TextValidatorsTests
    {
        [Fact]
        public void ValidateCreate_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new CreateTextCommandValidator();
            var command = new CreateTextCommand(CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCreate_WhenValuesAreAtLimits_ShouldBeValid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.TextContent = new string('a', TextEntity.TextContentMaxLength);
            dto.Title = new string('a', 300);
            dto.AdditionalText = new string('a', 500);
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCreate_WhenAdditionalTextIsNull_ShouldBeValid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.AdditionalText = null;
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCreate_WhenTextContentIsEmpty_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.TextContent = string.Empty;
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.TextContent)}");
        }

        [Fact]
        public void ValidateCreate_WhenTitleIsEmpty_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.Title = string.Empty;
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.Title)}");
        }

        [Fact]
        public void ValidateCreate_WhenTextContentExceedsMaxLength_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.TextContent = new string('a', TextEntity.TextContentMaxLength + 1);
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.TextContent)}");
        }

        [Fact]
        public void ValidateCreate_WhenTitleExceedsMaxLength_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.Title = new string('a', 301);
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.Title)}");
        }

        [Fact]
        public void ValidateCreate_WhenAdditionalTextExceedsMaxLength_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.AdditionalText = new string('a', 501);
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.AdditionalText)}");
        }

        [Fact]
        public void ValidateCreate_WhenStreetcodeIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new CreateTextCommandValidator();
            var dto = CreateValidDto();
            dto.StreetcodeId = 0;
            var command = new CreateTextCommand(dto);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName ==
                    $"{nameof(CreateTextCommand.TextCreateDto)}.{nameof(TextCreateDTO.StreetcodeId)}");
        }

        private static TextCreateDTO CreateValidDto()
        {
            return new TextCreateDTO
            {
                StreetcodeId = 1,
                Title = "Valid title",
                TextContent = "Valid text content",
                AdditionalText = "Valid additional text",
            };
        }
    }
}
