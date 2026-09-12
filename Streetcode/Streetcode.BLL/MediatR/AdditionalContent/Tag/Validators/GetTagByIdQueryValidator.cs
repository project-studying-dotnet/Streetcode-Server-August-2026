using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetById;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class GetTagByIdQueryValidator
    : AbstractValidator<GetTagByIdQuery>
{
    public GetTagByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Tag");
    }
}