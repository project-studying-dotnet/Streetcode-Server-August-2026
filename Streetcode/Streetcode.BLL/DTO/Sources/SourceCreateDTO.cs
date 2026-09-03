using Streetcode.BLL.DTO.Media.Images;

namespace Streetcode.BLL.DTO.Sources;

public record SourceCreateDTO(
    int StreetcodeId,
    string Text,
    int? SourceLinkCategoryId,
    string? NewCategoryTitle,
    ImageFileBaseCreateDTO? NewCategoryImage);
