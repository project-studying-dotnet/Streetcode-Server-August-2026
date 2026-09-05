using SkiaSharp;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.Interfaces.Sources;

namespace Streetcode.BLL.Services.Sources;

public sealed class SourceCategoryImageProcessor
    : ISourceCategoryImageProcessor
{
    public ImageFileBaseCreateDTO ConvertToGrayscale(
        ImageFileBaseCreateDTO image)
    {
        byte[] imageBytes =
            Convert.FromBase64String(image.BaseFormat!);

        using SKBitmap sourceBitmap = DecodeImage(imageBytes);

        using var grayscaleBitmap =
            new SKBitmap(sourceBitmap.Info);

        float[] grayscaleMatrix =
        [
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0,       0,       0,       1, 0,
        ];

        using SKColorFilter colorFilter =
            SKColorFilter.CreateColorMatrix(grayscaleMatrix);

        using var paint = new SKPaint
        {
            ColorFilter = colorFilter,
        };

        using var canvas = new SKCanvas(grayscaleBitmap);

        canvas.DrawBitmap(
            sourceBitmap,
            0,
            0,
            paint);

        canvas.Flush();

        (
                SKEncodedImageFormat outputFormat,
                string outputMimeType,
                string outputExtension) =
            GetOutputFormat(image.Extension);

        using SKImage grayscaleImage =
            SKImage.FromBitmap(grayscaleBitmap);

        using SKData encodedImage =
            grayscaleImage.Encode(outputFormat, 100)
            ?? throw new InvalidOperationException(
                "Failed to encode grayscale source category image.");

        byte[] grayscaleBytes = encodedImage.ToArray();

        return new ImageFileBaseCreateDTO
        {
            Title = image.Title,
            Alt = image.Alt,
            BaseFormat = Convert.ToBase64String(grayscaleBytes),
            MimeType = outputMimeType,
            Extension = outputExtension,
        };
    }

    private static SKBitmap DecodeImage(byte[] imageBytes)
    {
        try
        {
            return SKBitmap.Decode(imageBytes)
                ?? throw new InvalidOperationException(
                    "Failed to decode source category image.");
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Failed to decode source category image.",
                exception);
        }
    }

    private static (
        SKEncodedImageFormat Format,
        string MimeType,
        string Extension) GetOutputFormat(
            string? extension)
    {
        return extension?
                .Trim()
                .TrimStart('.')
                .ToLowerInvariant() switch
            {
                "jpg" => (
                    SKEncodedImageFormat.Jpeg,
                    "image/jpeg",
                    "jpg"),

                "jpeg" => (
                    SKEncodedImageFormat.Jpeg,
                    "image/jpeg",
                    "jpeg"),

                "png" => (
                    SKEncodedImageFormat.Png,
                    "image/png",
                    "png"),

                "gif" => (
                    SKEncodedImageFormat.Png,
                    "image/png",
                    "png"),

                _ => throw new InvalidOperationException(
                    "Unsupported source category image format."),
            };
    }
}
