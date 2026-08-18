using FluentValidation;
using Streetcode.BLL.MediatR.Media.StreetcodeArt.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.StreetcodeArt.Validators;

public sealed class GetStreetcodeArtByStreetcodeIdQueryValidator
    : AbstractValidator<GetStreetcodeArtByStreetcodeIdQuery>
{
    public GetStreetcodeArtByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}