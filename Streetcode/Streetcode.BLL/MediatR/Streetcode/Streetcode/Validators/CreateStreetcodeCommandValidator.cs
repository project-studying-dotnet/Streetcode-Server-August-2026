using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Create;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class CreateStreetcodeCommandValidator
    : AbstractValidator<CreateStreetcodeCommand>
{
    public CreateStreetcodeCommandValidator()
    {
        RuleFor(command => command.newStreetcode.Index)
            .InclusiveBetween(1, 9999);

        RuleFor(command => command.newStreetcode.StreetcodeType)
            .NotNull()
            .WithMessage("StreetcodeType is required.");

        RuleFor(command => command.newStreetcode.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MustNotExceedLength(StreetcodeContent.TitleMaxLength, "Title");

        RuleFor(command => command.newStreetcode.DateString)
            .NotEmpty()
            .WithMessage("DateString is required.")
            .MustNotExceedLength(50, "DateString");

        RuleFor(command => command.newStreetcode.FirstName)
            .MustNotExceedLength(50, "FirstName");

        RuleFor(command => command.newStreetcode.LastName)
            .MustNotExceedLength(50, "LastName");

        When(command => command.newStreetcode.StreetcodeType == StreetcodeType.Person, () =>
        {
            RuleFor(command => command.newStreetcode.FirstName)
                .NotEmpty()
                .WithMessage("FirstName is required for a Person streetcode.");

            RuleFor(command => command.newStreetcode.LastName)
                .NotEmpty()
                .WithMessage("LastName is required for a Person streetcode.");
        });

        RuleFor(command => command.newStreetcode.ShortDescription)
            .MustNotExceedLength(StreetcodeContent.ShortDescriptionMaxLength, "ShortDescription");

        RuleFor(command => command.newStreetcode.TransliterationUrl)
            .NotEmpty()
            .WithMessage("TransliterationUrl is required.")
            .MustNotExceedLength(100, "TransliterationUrl")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("TransliterationUrl must contain only lowercase letters, numbers, and hyphens.");

        RuleForEach(command => command.newStreetcode.Tags)
            .ChildRules(tag => tag.RuleFor(t => t.Title).MustNotExceedLength(50, "Tag title"));
    }
}
