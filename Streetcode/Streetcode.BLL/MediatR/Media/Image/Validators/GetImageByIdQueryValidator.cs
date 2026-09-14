using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Image.GetById;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class GetImageByIdQueryValidator
    : AbstractValidator<GetImageByIdQuery>
{
    public GetImageByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Image");
    }
}