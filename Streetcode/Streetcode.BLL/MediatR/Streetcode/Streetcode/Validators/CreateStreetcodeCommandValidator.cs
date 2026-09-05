using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.DeleteSoft;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class CreateStreetcodeCommandValidator
    : AbstractValidator<CreateStreetcodeCommand>
{
    public CreateStreetcodeCommandValidator()
    {
        RuleFor(command => command.newStreetcode.Index)
            .InclusiveBetween(1, 9999);

        RuleFor(command => command.newStreetcode.Title)
            .MustNotExceedLength(100, "Title");

        RuleFor(command => command.newStreetcode.FirstName)
            .MustNotExceedLength(50, "FirstName");

        RuleFor(command => command.newStreetcode.LastName)
            .MustNotExceedLength(50, "LastName");

        RuleFor(command => command.newStreetcode.Teaser)
            .MustNotExceedLength(33, "Teaser");

        RuleFor(command => command.newStreetcode.TransliterationUrl)
            .MustNotExceedLength(100, "TransliterationUrl")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("TransliterationUrl must contain only lowercase letters, numbers, and hyphens.");

        RuleForEach(command => command.newStreetcode.Tags)
            .ChildRules(tag => tag.RuleFor(t => t.Title).MustNotExceedLength(50, "Tag title"));
    }
}
