using FluentValidation;
using Streetcode.BLL.MediatR.Media.Art.GetById;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class GetArtByIdQueryValidator
    : AbstractValidator<GetArtByIdQuery>
{
    public GetArtByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Art ID must be greater than 0.");
    }
}