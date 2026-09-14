// <copyright file="SourceCategoryImageProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Services.Sources
{
    using global::Streetcode.BLL.DTO.Media.Images;
    using global::Streetcode.BLL.Services.Sources;
    using SkiaSharp;
    using Xunit;

    public class SourceCategoryImageProcessorTests
    {
        [Fact]
        public void ConvertToGrayscale_WhenPngIsValid_ShouldReturnGrayscaleImage()
        {
            var input = CreatePngInput(
                new SKColor(255, 100, 50, 128));
            var processor = new SourceCategoryImageProcessor();

            ImageFileBaseCreateDTO result =
                processor.ConvertToGrayscale(input);

            byte[] resultBytes =
                Convert.FromBase64String(result.BaseFormat!);
            using SKBitmap resultBitmap =
                SKBitmap.Decode(resultBytes);
            SKColor pixel = resultBitmap.GetPixel(0, 0);

            Assert.InRange(
                Math.Abs((int)pixel.Red - pixel.Green),
                0,
                1);
            Assert.InRange(
                Math.Abs((int)pixel.Green - pixel.Blue),
                0,
                1);
            Assert.Equal(128, pixel.Alpha);
            Assert.Equal("image/png", result.MimeType);
            Assert.Equal("png", result.Extension);
            Assert.Equal(input.Title, result.Title);
            Assert.Equal(input.Alt, result.Alt);
            Assert.NotSame(input, result);
        }

        [Fact]
        public void ConvertToGrayscale_WhenGifExtensionProvided_ShouldReturnPngMetadata()
        {
            var input = CreatePngInput(SKColors.Red);
            input.MimeType = "image/gif";
            input.Extension = "gif";
            var processor = new SourceCategoryImageProcessor();

            ImageFileBaseCreateDTO result =
                processor.ConvertToGrayscale(input);

            Assert.Equal("image/png", result.MimeType);
            Assert.Equal("png", result.Extension);

            byte[] resultBytes =
                Convert.FromBase64String(result.BaseFormat!);
            using SKBitmap resultBitmap =
                SKBitmap.Decode(resultBytes);

            Assert.Equal(1, resultBitmap.Width);
            Assert.Equal(1, resultBitmap.Height);
        }

        [Fact]
        public void ConvertToGrayscale_WhenExtensionIsUnsupported_ShouldThrowException()
        {
            var input = CreatePngInput(SKColors.Blue);
            input.Extension = "bmp";
            var processor = new SourceCategoryImageProcessor();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => processor.ConvertToGrayscale(input));

            Assert.Equal(
                "Unsupported source category image format.",
                exception.Message);
        }

        [Fact]
        public void ConvertToGrayscale_WhenImageCannotBeDecoded_ShouldThrowException()
        {
            var input = new ImageFileBaseCreateDTO
            {
                BaseFormat = Convert.ToBase64String(
                    new byte[] { 1, 2, 3 }),
                MimeType = "image/png",
                Extension = "png",
            };
            var processor = new SourceCategoryImageProcessor();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => processor.ConvertToGrayscale(input));

            Assert.Equal(
                "Failed to decode source category image.",
                exception.Message);
        }

        private static ImageFileBaseCreateDTO CreatePngInput(
            SKColor color)
        {
            var imageInfo = new SKImageInfo(
                width: 1,
                height: 1,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Unpremul);
            using var bitmap = new SKBitmap(imageInfo);
            bitmap.SetPixel(0, 0, color);

            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData encodedImage = image.Encode(
                SKEncodedImageFormat.Png,
                quality: 100);

            return new ImageFileBaseCreateDTO
            {
                Title = "Source category image",
                Alt = "Source category alt",
                BaseFormat = Convert.ToBase64String(
                    encodedImage.ToArray()),
                MimeType = "image/png",
                Extension = "png",
            };
        }
    }
}
