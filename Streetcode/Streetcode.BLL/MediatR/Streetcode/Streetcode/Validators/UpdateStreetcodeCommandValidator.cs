using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Update;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class UpdateStreetcodeCommandValidator
    : AbstractValidator<UpdateStreetcodeCommand>
{
    public UpdateStreetcodeCommandValidator()
    {
        RuleFor(command => command.Id)
            .MustBeValidId("Streetcode");

        RuleFor(command => command.updatedStreetcode.Index)
            .InclusiveBetween(1, 9999);

        RuleFor(command => command.updatedStreetcode.Title)
            .MustNotExceedLength(100, "Title");

        RuleFor(command => command.updatedStreetcode.FirstName)
            .MustNotExceedLength(50, "FirstName");

        RuleFor(command => command.updatedStreetcode.LastName)
            .MustNotExceedLength(50, "LastName");

        RuleFor(command => command.updatedStreetcode.Teaser)
            .MustNotExceedLength(33, "Teaser");

        RuleFor(command => command.updatedStreetcode.TransliterationUrl)
            .MustNotExceedLength(100, "TransliterationUrl")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("URL may only contain lowercase latin letters, numbers, and hyphens.");

        RuleForEach(command => command.updatedStreetcode.Tags)
            .ChildRules(tag => tag.RuleFor(t => t.Title).MustNotExceedLength(50, "Tag title"));
    }
}