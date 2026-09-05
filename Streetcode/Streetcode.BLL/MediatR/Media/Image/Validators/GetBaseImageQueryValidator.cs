using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Media.Image.GetBaseImage;

namespace Streetcode.BLL.MediatR.Media.Image.Validators;

public sealed class GetBaseImageQueryValidator
    : AbstractValidator<GetBaseImageQuery>
{
    public GetBaseImageQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Image");
    }
}