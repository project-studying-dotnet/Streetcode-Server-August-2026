using FluentValidation;
using Streetcode.BLL.DTO.Media.Art;
using Streetcode.BLL.MediatR.Validators;
using ArtEntity = Streetcode.DAL.Entities.Media.Images.Art;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class ArtUpdateCreateDtoValidator : AbstractValidator<ArtUpdateCreateDto>
{
    public ArtUpdateCreateDtoValidator()
    {
        RuleFor(art => art.ImageId)
            .MustBeValidId("Image");

        RuleFor(art => art.Title)
            .MustNotExceedLength(
                ArtEntity.TitleMaxLength,
                "Title");

        RuleFor(art => art.Description)
            .MustNotExceedLength(
                ArtEntity.DescriptionMaxLength,
                "Description");
    }
}
