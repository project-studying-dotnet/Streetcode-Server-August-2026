using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class GetTagByTitleQueryValidator
    : AbstractValidator<GetTagByTitleQuery>
{
    public GetTagByTitleQueryValidator()
    {
        RuleFor(query => query.Title)
            .NotEmpty()
            .WithMessage("Tag title is required.")
            .MaximumLength(50)
            .WithMessage(
                "Tag title must not exceed 50 characters.");
    }
}