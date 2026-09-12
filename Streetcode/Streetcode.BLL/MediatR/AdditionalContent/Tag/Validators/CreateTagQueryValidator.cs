using FluentValidation;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.Create;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class CreateTagQueryValidator
    : AbstractValidator<CreateTagQuery>
{
    public CreateTagQueryValidator(
        IValidator<CreateTagDTO> tagValidator)
    {
        RuleFor(query => query.tag)
            .NotNull()
            .WithMessage("Tag data is required.")
            .SetValidator(tagValidator);
    }
}