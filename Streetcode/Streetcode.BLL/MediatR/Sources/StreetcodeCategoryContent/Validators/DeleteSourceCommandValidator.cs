using FluentValidation;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Delete;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;

public sealed class DeleteSourceCommandValidator
    : AbstractValidator<DeleteSourceCommand>
{
    public DeleteSourceCommandValidator()
    {
        RuleFor(command => command.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(command => command.SourceLinkCategoryId)
            .MustBeValidId("Source category");
    }
}
