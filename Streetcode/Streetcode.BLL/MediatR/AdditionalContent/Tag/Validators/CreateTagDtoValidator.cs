using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Tag;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class CreateTagDtoValidator
    : AbstractValidator<CreateTagDTO>
{
    public CreateTagDtoValidator()
    {
        RuleFor(tag => tag.Title)
            .NotEmpty()
            .WithMessage("Tag title is required.")
            .MaximumLength(50)
            .WithMessage(
                "Tag title must not exceed 50 characters.");
    }
}