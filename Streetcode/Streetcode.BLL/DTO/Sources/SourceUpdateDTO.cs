namespace Streetcode.BLL.DTO.Sources;

public record SourceUpdateDTO(
    int StreetcodeId,
    int SourceLinkCategoryId,
    string Text);
