using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.GetAllByTermId;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class GetAllRelatedTermsByTermIdQueryValidator
    : AbstractValidator<GetAllRelatedTermsByTermIdQuery>
{
    public GetAllRelatedTermsByTermIdQueryValidator()
    {
        RuleFor(query => query.id)
            .MustBeValidId("Term");
    }
}