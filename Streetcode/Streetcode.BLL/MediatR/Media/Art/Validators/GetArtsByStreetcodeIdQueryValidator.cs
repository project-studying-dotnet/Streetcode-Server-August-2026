using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Art.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class GetArtsByStreetcodeIdQueryValidator
    : AbstractValidator<GetArtsByStreetcodeIdQuery>
{
    public GetArtsByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}