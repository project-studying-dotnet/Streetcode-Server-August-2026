using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class GetTagByStreetcodeIdQueryValidator
    : AbstractValidator<GetTagByStreetcodeIdQuery>
{
    public GetTagByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage(
                "Streetcode ID must be greater than zero.");
    }
}