using Streetcode.BLL.DTO.Media.Images;

namespace Streetcode.BLL.Interfaces.Sources;

public interface ISourceCategoryImageProcessor
{
    ImageFileBaseCreateDTO ConvertToGrayscale(ImageFileBaseCreateDTO image);
}
