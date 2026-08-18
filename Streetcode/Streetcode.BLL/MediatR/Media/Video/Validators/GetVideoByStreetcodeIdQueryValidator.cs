using FluentValidation;
using Streetcode.BLL.MediatR.Media.Video.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Media.Video.Validators;

public sealed class GetVideoByStreetcodeIdQueryValidator
    : AbstractValidator<GetVideoByStreetcodeIdQuery>
{
    public GetVideoByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}