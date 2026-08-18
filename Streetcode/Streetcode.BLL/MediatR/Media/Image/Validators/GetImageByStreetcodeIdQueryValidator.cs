using FluentValidation;
using Streetcode.BLL.MediatR.Media.Image.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class GetImageByStreetcodeIdQueryValidator
    : AbstractValidator<GetImageByStreetcodeIdQuery>
{
    public GetImageByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}