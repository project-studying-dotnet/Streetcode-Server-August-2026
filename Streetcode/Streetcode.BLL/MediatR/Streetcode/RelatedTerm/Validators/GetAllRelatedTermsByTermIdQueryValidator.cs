using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.GetAllByTermId;

namespace Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Validators;

public sealed class GetAllRelatedTermsByTermIdQueryValidator
    : AbstractValidator<GetAllRelatedTermsByTermIdQuery>
{
    public GetAllRelatedTermsByTermIdQueryValidator()
    {
        RuleFor(query => query.id)
            .GreaterThan(0)
            .WithMessage("Term ID must be greater than 0.");
    }
}