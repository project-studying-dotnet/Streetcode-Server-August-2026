using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetShortById;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeShortByIdQueryValidator
    : AbstractValidator<GetStreetcodeShortByIdQuery>
{
    public GetStreetcodeShortByIdQueryValidator()
    {
        RuleFor(query => query.id)
            .MustBeValidId("Streetcode");
    }
}