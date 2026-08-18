using FluentValidation;
using Streetcode.BLL.MediatR.Media.Video.GetById;

namespace Streetcode.BLL.MediatR.Media.Video.Validators;

public sealed class GetVideoByIdQueryValidator
    : AbstractValidator<GetVideoByIdQuery>
{
    public GetVideoByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Video ID must be greater than 0.");
    }
}