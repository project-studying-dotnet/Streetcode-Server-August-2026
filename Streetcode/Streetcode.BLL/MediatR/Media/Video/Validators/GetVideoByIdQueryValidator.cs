using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Video.GetById;

namespace Streetcode.BLL.MediatR.Media.Video.Validators;

public sealed class GetVideoByIdQueryValidator
    : AbstractValidator<GetVideoByIdQuery>
{
    public GetVideoByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Video");
    }
}