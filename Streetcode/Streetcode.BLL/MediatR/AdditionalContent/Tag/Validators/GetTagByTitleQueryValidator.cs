using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetByStreetcodeId;
using TagEntity = Streetcode.DAL.Entities.AdditionalContent.Tag;

namespace Streetcode.BLL.MediatR.AdditionalContent.Tag.Validators;

public sealed class GetTagByTitleQueryValidator
    : AbstractValidator<GetTagByTitleQuery>
{
    public GetTagByTitleQueryValidator()
    {
        RuleFor(query => query.Title)
            .NotEmpty()
            .WithMessage("Tag title is required.")
            .MustNotExceedLength(TagEntity.TitleMaxLength, "Tag title");
    }
}