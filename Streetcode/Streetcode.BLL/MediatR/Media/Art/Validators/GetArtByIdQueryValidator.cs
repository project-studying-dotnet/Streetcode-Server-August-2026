using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Art.GetById;

namespace Streetcode.BLL.MediatR.Media.Art.Validators;

public sealed class GetArtByIdQueryValidator
    : AbstractValidator<GetArtByIdQuery>
{
    public GetArtByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Art");
    }
}