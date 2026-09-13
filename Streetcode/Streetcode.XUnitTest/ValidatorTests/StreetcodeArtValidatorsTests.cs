namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Media.Art;
    using Streetcode.BLL.Enums;
    using Streetcode.BLL.MediatR.Media.StreetcodeArt.Attach;
    using Streetcode.BLL.MediatR.Media.StreetcodeArt.Detach;
    using Streetcode.BLL.MediatR.Media.StreetcodeArt.Move;
    using Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;
    using Xunit;

    public class StreetcodeArtValidatorsTests
    {
        [Fact]
        public void ValidateDto_WhenValid_ShouldBeValid()
        {
            var validator = new StreetcodeArtAttachDtoValidator();

            var result = validator.Validate(CreateValidDto());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDto_WhenStreetcodeIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new StreetcodeArtAttachDtoValidator();
            var dto = CreateValidDto();
            dto.StreetcodeId = 0;

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(StreetcodeArtAttachDto.StreetcodeId));
        }

        [Fact]
        public void ValidateDto_WhenArtIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new StreetcodeArtAttachDtoValidator();
            var dto = CreateValidDto();
            dto.ArtId = 0;

            var result = validator.Validate(dto);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(StreetcodeArtAttachDto.ArtId));
        }

        [Fact]
        public void ValidateAttach_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new AttachStreetcodeArtCommandValidator(
                new StreetcodeArtAttachDtoValidator());
            var command = new AttachStreetcodeArtCommand(CreateValidDto());

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAttach_WhenAttachIsNull_ShouldBeInvalid()
        {
            var validator = new AttachStreetcodeArtCommandValidator(
                new StreetcodeArtAttachDtoValidator());
            var command = new AttachStreetcodeArtCommand(null!);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(AttachStreetcodeArtCommand.Attach));
        }

        [Fact]
        public void ValidateDetach_WhenIdsAreValid_ShouldBeValid()
        {
            var validator = new DetachStreetcodeArtCommandValidator();
            var command = new DetachStreetcodeArtCommand(1, 1);

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateDetach_WhenStreetcodeIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new DetachStreetcodeArtCommandValidator();
            var command = new DetachStreetcodeArtCommand(0, 1);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(DetachStreetcodeArtCommand.StreetcodeId));
        }

        [Fact]
        public void ValidateDetach_WhenArtIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new DetachStreetcodeArtCommandValidator();
            var command = new DetachStreetcodeArtCommand(1, 0);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(DetachStreetcodeArtCommand.ArtId));
        }

        [Fact]
        public void ValidateMove_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new MoveStreetcodeArtCommandValidator();
            var command = new MoveStreetcodeArtCommand(1, 1, MoveDirection.Forward);

            var result = validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateMove_WhenStreetcodeIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new MoveStreetcodeArtCommandValidator();
            var command = new MoveStreetcodeArtCommand(0, 1, MoveDirection.Forward);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(MoveStreetcodeArtCommand.StreetcodeId));
        }

        [Fact]
        public void ValidateMove_WhenArtIdIsNotPositive_ShouldBeInvalid()
        {
            var validator = new MoveStreetcodeArtCommandValidator();
            var command = new MoveStreetcodeArtCommand(1, 0, MoveDirection.Forward);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(MoveStreetcodeArtCommand.ArtId));
        }

        [Fact]
        public void ValidateMove_WhenDirectionIsInvalid_ShouldBeInvalid()
        {
            var validator = new MoveStreetcodeArtCommandValidator();
            var command = new MoveStreetcodeArtCommand(1, 1, (MoveDirection)99);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(MoveStreetcodeArtCommand.Direction));
        }

        [Fact]
        public void ValidateMove_WhenDirectionIsMissing_ShouldBeInvalid()
        {
            var validator = new MoveStreetcodeArtCommandValidator();
            var command = new MoveStreetcodeArtCommand(1, 1, null);

            var result = validator.Validate(command);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(MoveStreetcodeArtCommand.Direction) &&
                    error.ErrorMessage == "Direction is required.");
        }

        private static StreetcodeArtAttachDto CreateValidDto()
        {
            return new StreetcodeArtAttachDto
            {
                StreetcodeId = 1,
                ArtId = 1,
            };
        }
    }
}
