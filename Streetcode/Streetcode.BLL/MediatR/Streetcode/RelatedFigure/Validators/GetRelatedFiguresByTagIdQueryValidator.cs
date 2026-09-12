using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedFigure.GetByTagId;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedFigure.Validators;

public sealed class GetRelatedFiguresByTagIdQueryValidator
    : AbstractValidator<GetRelatedFiguresByTagIdQuery>
{
    public GetRelatedFiguresByTagIdQueryValidator()
    {
        RuleFor(query => query.tagId)
            .MustBeValidId("Tag");
    }
}