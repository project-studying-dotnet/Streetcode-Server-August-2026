using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.AdditionalContent.Subtitle.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.AdditionalContent.Subtitle.Validators;

public sealed class GetSubtitlesByStreetcodeIdQueryValidator
    : AbstractValidator<GetSubtitlesByStreetcodeIdQuery>
{
    public GetSubtitlesByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}