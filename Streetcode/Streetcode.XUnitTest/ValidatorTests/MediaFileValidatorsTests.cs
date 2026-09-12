// <copyright file="MediaFileValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Media.Audio;
    using Streetcode.BLL.DTO.Media.Images;
    using Streetcode.BLL.MediatR.Media.Audio.Create;
    using Streetcode.BLL.MediatR.Media.Audio.Validators;
    using Streetcode.BLL.MediatR.Media.Image.Create;
    using Streetcode.BLL.MediatR.Media.Image.Validators;
    using Xunit;

    public class MediaFileValidatorsTests
    {
        [Theory]
        [InlineData("image/jpeg", "jpg")]
        [InlineData("image/jpeg", "jpeg")]
        [InlineData("image/png", "png")]
        [InlineData("image/gif", "gif")]
        [InlineData("IMAGE/PNG", ".PNG")]
        public void ValidateImage_WhenFileTypePairIsSupported_ShouldBeValid(
            string mimeType,
            string extension)
        {
            var validator = new ImageFileBaseCreateDtoValidator();
            ImageFileBaseCreateDTO image = CreateValidImage();
            image.MimeType = mimeType;
            image.Extension = extension;

            var result = validator.Validate(image);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("image/jpeg", "png")]
        [InlineData("application/octet-stream", "jpg")]
        [InlineData("image/webp", "webp")]
        public void ValidateImage_WhenFileTypePairIsUnsupported_ShouldBeInvalid(
            string mimeType,
            string extension)
        {
            var validator = new ImageFileBaseCreateDtoValidator();
            ImageFileBaseCreateDTO image = CreateValidImage();
            image.MimeType = mimeType;
            image.Extension = extension;

            var result = validator.Validate(image);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateImage_WhenBase64IsInvalid_ShouldBeInvalid()
        {
            var validator = new ImageFileBaseCreateDtoValidator();
            ImageFileBaseCreateDTO image = CreateValidImage();
            image.BaseFormat = "not-base64";

            var result = validator.Validate(image);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(image.BaseFormat));
        }

        [Fact]
        public void ValidateAudio_WhenFileTypePairIsSupported_ShouldBeValid()
        {
            var validator = new AudioFileBaseCreateDtoValidator();

            var result = validator.Validate(CreateValidAudio());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAudio_WhenMimeAndExtensionDoNotMatch_ShouldBeInvalid()
        {
            var validator = new AudioFileBaseCreateDtoValidator();
            AudioFileBaseCreateDTO audio = CreateValidAudio();
            audio.Extension = "wav";

            var result = validator.Validate(audio);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateCommands_WhenNestedBase64IsInvalid_ShouldIncludeNestedErrors()
        {
            var imageDtoValidator = new ImageFileBaseCreateDtoValidator();
            var imageCommandValidator = new CreateImageCommandValidator(
                imageDtoValidator);
            ImageFileBaseCreateDTO image = CreateValidImage();
            image.BaseFormat = "invalid";

            var audioDtoValidator = new AudioFileBaseCreateDtoValidator();
            var audioCommandValidator = new CreateAudioCommandValidator(
                audioDtoValidator);
            AudioFileBaseCreateDTO audio = CreateValidAudio();
            audio.BaseFormat = "invalid";

            var imageResult = imageCommandValidator.Validate(
                new CreateImageCommand(image));
            var audioResult = audioCommandValidator.Validate(
                new CreateAudioCommand(audio));

            Assert.Contains(
                imageResult.Errors,
                error => error.PropertyName == "Image.BaseFormat");
            Assert.Contains(
                audioResult.Errors,
                error => error.PropertyName == "Audio.BaseFormat");
        }

        private static ImageFileBaseCreateDTO CreateValidImage()
        {
            return new ImageFileBaseCreateDTO
            {
                BaseFormat = "AQID",
                MimeType = "image/jpeg",
                Extension = "jpg",
            };
        }

        private static AudioFileBaseCreateDTO CreateValidAudio()
        {
            return new AudioFileBaseCreateDTO
            {
                BaseFormat = "AQID",
                MimeType = "audio/mpeg",
                Extension = "mp3",
            };
        }
    }
}
