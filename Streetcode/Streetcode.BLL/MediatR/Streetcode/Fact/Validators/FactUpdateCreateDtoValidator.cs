using FluentValidation;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Fact.Validators;

public sealed class FactUpdateCreateDtoValidator
    : AbstractValidator<FactUpdateCreateDto>
{
    public FactUpdateCreateDtoValidator()
    {
        RuleFor(fact => fact.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MustNotExceedLength(68, "Title");

        RuleFor(fact => fact.FactContent)
            .NotEmpty()
            .WithMessage("Fact content is required.")
            .MustNotExceedLength(600, "Fact content");

        RuleFor(fact => fact.ImageAlt)
            .MustNotExceedLength(200, "Image alt");

        RuleFor(fact => fact.ImageId)
            .MustBeValidId("Image");

        RuleFor(fact => fact.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}
