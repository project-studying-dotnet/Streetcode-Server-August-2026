using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByTransliterationUrl;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByTransliterationUrlQueryValidator
    : AbstractValidator<GetStreetcodeByTransliterationUrlQuery>
{
    public GetStreetcodeByTransliterationUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage("Transliteration URL is required.")
            .MustNotExceedLength(
                StreetcodeContent.TransliterationUrlMaxLength,
                "Transliteration URL");
    }
}