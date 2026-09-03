using FluentValidation;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.MediatR.Validators;
using SourceEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;

public sealed class SourceUpdateDtoValidator
    : AbstractValidator<SourceUpdateDTO>
{
    public SourceUpdateDtoValidator()
    {
        RuleFor(source => source.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(source => source.SourceLinkCategoryId)
            .MustBeValidId("Source category");

        RuleFor(source => source.Text)
            .MustNotExceedLength(
                SourceEntity.TextMaxLength,
                "Source text");
    }
}
