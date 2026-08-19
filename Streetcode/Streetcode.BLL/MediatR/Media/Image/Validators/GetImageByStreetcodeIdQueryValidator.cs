using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Image.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class GetImageByStreetcodeIdQueryValidator
    : AbstractValidator<GetImageByStreetcodeIdQuery>
{
    public GetImageByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}