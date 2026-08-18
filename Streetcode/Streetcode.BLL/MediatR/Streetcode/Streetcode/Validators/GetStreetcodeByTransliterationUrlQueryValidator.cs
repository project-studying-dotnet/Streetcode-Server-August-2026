using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByTransliterationUrl;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByTransliterationUrlQueryValidator
    : AbstractValidator<GetStreetcodeByTransliterationUrlQuery>
{
    public GetStreetcodeByTransliterationUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage("Transliteration URL cannot be empty.")
            .MaximumLength(150)
            .WithMessage("Transliteration URL cannot exceed 150 characters.");
    }
}