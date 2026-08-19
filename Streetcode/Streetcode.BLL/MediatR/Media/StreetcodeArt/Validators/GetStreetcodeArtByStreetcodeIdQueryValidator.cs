using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class GetStreetcodeArtByStreetcodeIdQueryValidator
    : AbstractValidator<GetStreetcodeArtByStreetcodeIdQuery>
{
    public GetStreetcodeArtByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}