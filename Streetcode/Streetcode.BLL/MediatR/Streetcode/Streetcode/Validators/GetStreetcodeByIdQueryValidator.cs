using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetById;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetStreetcodeByIdQueryValidator
    : AbstractValidator<GetStreetcodeByIdQuery>
{
    public GetStreetcodeByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Streetcode");
    }
}