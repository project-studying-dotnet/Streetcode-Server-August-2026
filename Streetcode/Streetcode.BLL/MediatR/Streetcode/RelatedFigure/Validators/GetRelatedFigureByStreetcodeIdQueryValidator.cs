using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedFigure.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Validators;

public sealed class GetRelatedFigureByStreetcodeIdQueryValidator
    : AbstractValidator<GetRelatedFigureByStreetcodeIdQuery>
{
    public GetRelatedFigureByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}