using FluentValidation;
using Streetcode.BLL.MediatR.Media.Image.GetById;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class GetImageByIdQueryValidator
    : AbstractValidator<GetImageByIdQuery>
{
    public GetImageByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Image ID must be greater than 0.");
    }
}