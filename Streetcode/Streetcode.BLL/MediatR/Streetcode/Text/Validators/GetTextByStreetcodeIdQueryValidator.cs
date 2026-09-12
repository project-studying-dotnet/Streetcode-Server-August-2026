using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Streetcode.Text.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Validators;

public sealed class GetTextByStreetcodeIdQueryValidator
    : AbstractValidator<GetTextByStreetcodeIdQuery>
{
    public GetTextByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}