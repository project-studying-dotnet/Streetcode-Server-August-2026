using FluentValidation;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Create;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;

public sealed class CreateSourceCommandValidator
    : AbstractValidator<CreateSourceCommand>
{
    public CreateSourceCommandValidator(
        IValidator<SourceCreateDTO> sourceValidator)
    {
        RuleFor(command => command.SourceCreateDto)
            .NotNull()
            .WithMessage("Source is required.")
            .SetValidator(sourceValidator);
    }
}
