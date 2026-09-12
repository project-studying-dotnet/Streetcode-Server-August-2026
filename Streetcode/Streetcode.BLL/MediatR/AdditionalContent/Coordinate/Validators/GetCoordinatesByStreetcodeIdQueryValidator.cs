using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.Coordinate.GetByStreetcodeId;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.AdditionalContent.Coordinate.Validators;

public sealed class GetCoordinatesByStreetcodeIdQueryValidator
    : AbstractValidator<GetCoordinatesByStreetcodeIdQuery>
{
    public GetCoordinatesByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}
