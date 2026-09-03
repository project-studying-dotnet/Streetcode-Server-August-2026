using FluentValidation;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Update;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;

public sealed class UpdateSourceCommandValidator
    : AbstractValidator<UpdateSourceCommand>
{
    public UpdateSourceCommandValidator(
        IValidator<SourceUpdateDTO> sourceValidator)
    {
        RuleFor(command => command.SourceUpdateDto)
            .NotNull()
            .WithMessage("Source is required.")
            .SetValidator(sourceValidator);
    }
}
