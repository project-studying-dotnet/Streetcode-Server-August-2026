using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByTransliterationUrl;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByTransliterationUrlQueryValidator
    : AbstractValidator<GetStreetcodeByTransliterationUrlQuery>
{
    public GetStreetcodeByTransliterationUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithName("Transliteration URL")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(
                StreetcodeContent.TransliterationUrlMaxLength,
                "Transliteration URL");
    }
}