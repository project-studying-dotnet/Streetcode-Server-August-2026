using FluentValidation;
using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.MediatR.Validators;
using SourceCategoryEntity =
    Streetcode.DAL.Entities.Sources.SourceLinkCategory;
using SourceContentEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;

public sealed class SourceCreateDtoValidator
    : AbstractValidator<SourceCreateDTO>
{
    private const int MaxImageSizeBytes = 3 * 1024 * 1024;

    public SourceCreateDtoValidator(
        IValidator<ImageFileBaseCreateDTO> imageValidator)
    {
        RuleFor(source => source.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(source => source.Text)
            .MustNotExceedLength(
                SourceContentEntity.TextMaxLength,
                "Source text");

        RuleFor(t => t.SourceLinkCategoryId)
            .GreaterThan(0)
            .WithMessage("Source category ID must be greater than 0.")
            .When(source => source.SourceLinkCategoryId.HasValue);

        When(
            source => source.SourceLinkCategoryId.HasValue,
            () =>
            {
                RuleFor(source => source.NewCategoryTitle)
                    .Null()
                    .WithMessage(
                        "New category title must not be provided when an existing category is selected.");

                RuleFor(source => source.NewCategoryImage)
                    .Null()
                    .WithMessage(
                        "New category image must not be provided when an existing category is selected.");
            });

        When(
            source => !source.SourceLinkCategoryId.HasValue,
            () =>
            {
                RuleFor(source => source.NewCategoryTitle)
                    .NotEmpty()
                    .WithMessage("New category title is required.")
                    .MustNotExceedLength(
                        SourceCategoryEntity.TitleMaxLength,
                        "New category title");

                RuleFor(source => source.NewCategoryImage!)
                    .NotNull()
                    .WithMessage("New category image is required.")
                    .SetValidator(imageValidator);

                RuleFor(source => source.NewCategoryImage!.BaseFormat)
                    .Must(BeWithinSizeLimit)
                    .WithMessage(
                        "New category image must not exceed 3 MB.")
                    .When(source =>
                        source.NewCategoryImage is not null &&
                        !string.IsNullOrWhiteSpace(
                            source.NewCategoryImage.BaseFormat));
            });
    }

    private static bool BeWithinSizeLimit(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return true;
        }

        try
        {
            byte[] decodeBytes = Convert.FromBase64String(base64);

            return decodeBytes.Length <= MaxImageSizeBytes;
        }
        catch (FormatException)
        {
            return true;
        }
    }
}
