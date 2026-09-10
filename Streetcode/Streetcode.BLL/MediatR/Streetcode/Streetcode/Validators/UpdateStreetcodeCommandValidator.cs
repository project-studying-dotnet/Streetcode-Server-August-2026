using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Update;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;

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

        RuleFor(command => command.updatedStreetcode.StreetcodeType)
            .NotNull()
            .WithMessage("StreetcodeType is required.");

        RuleFor(command => command.updatedStreetcode.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MustNotExceedLength(StreetcodeContent.TitleMaxLength, "Title");

        RuleFor(command => command.updatedStreetcode.DateString)
            .NotEmpty()
            .WithMessage("DateString is required.")
            .MustNotExceedLength(50, "DateString");

        RuleFor(command => command.updatedStreetcode.FirstName)
            .MustNotExceedLength(50, "FirstName");

        RuleFor(command => command.updatedStreetcode.LastName)
            .MustNotExceedLength(50, "LastName");

        When(command => command.updatedStreetcode.StreetcodeType == StreetcodeType.Person, () =>
        {
            RuleFor(command => command.updatedStreetcode.FirstName)
                .NotEmpty()
                .WithMessage("FirstName is required for a Person streetcode.");

            RuleFor(command => command.updatedStreetcode.LastName)
                .NotEmpty()
                .WithMessage("LastName is required for a Person streetcode.");
        });

        RuleFor(command => command.updatedStreetcode.ShortDescription)
            .MustNotExceedLength(StreetcodeContent.ShortDescriptionMaxLength, "ShortDescription");

        RuleFor(command => command.updatedStreetcode.TransliterationUrl)
            .NotEmpty()
            .WithMessage("TransliterationUrl is required.")
            .MustNotExceedLength(100, "TransliterationUrl")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("URL may only contain lowercase latin letters, numbers, and hyphens.");

        RuleForEach(command => command.updatedStreetcode.Tags)
            .ChildRules(tag => tag.RuleFor(t => t.Title).MustNotExceedLength(50, "Tag title"));
    }
}