using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using TagEntity = Streetcode.DAL.Entities.AdditionalContent.Tag;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class CreateTagDtoValidator
    : AbstractValidator<CreateTagDTO>
{
    public CreateTagDtoValidator()
    {
        RuleFor(tag => tag.Title)
            .NotEmpty()
            .WithMessage("Tag title is required.")
            .MustNotExceedLength(TagEntity.TitleMaxLength, "Tag title");
    }
}