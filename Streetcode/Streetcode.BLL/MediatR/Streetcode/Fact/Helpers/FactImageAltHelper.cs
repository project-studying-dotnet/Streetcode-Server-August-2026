using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Media.Images;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Helpers;

internal static class FactImageAltHelper
{
    public static async Task SetAsync(
        Image image,
        string? imageAlt,
        IImageDetailsRepository imageDetailsRepository)
    {
        string? trimmedAlt = string.IsNullOrWhiteSpace(imageAlt)
            ? null
            : imageAlt.Trim();

        if (image.ImageDetails is null)
        {
            if (trimmedAlt is null)
            {
                return;
            }

            var imageDetails = new ImageDetails
            {
                ImageId = image.Id,
                Alt = trimmedAlt,
            };

            image.ImageDetails = imageDetails;
            await imageDetailsRepository.CreateAsync(imageDetails);
            return;
        }

        image.ImageDetails.Alt = trimmedAlt;
        imageDetailsRepository.Update(image.ImageDetails);
    }
}
