using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.Subtitle.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.AdditionalContent.Subtitle.Validators;

public sealed class GetSubtitleByStreetcodeIdQueryValidator
    : AbstractValidator<GetSubtitlesByStreetcodeIdQuery>
{
    public GetSubtitleByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than zero.");
    }
}