using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Toponyms.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetToponymsByStreetcodeIdQueryValidator
    : AbstractValidator<GetToponymsByStreetcodeIdQuery>
{
    public GetToponymsByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}