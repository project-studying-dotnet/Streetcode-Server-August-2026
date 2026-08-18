using FluentValidation;
using Streetcode.BLL.MediatR.AdditionalContent.GetById;

namespace Streetcode.BLL.MediatR.AdditionalContent.Subtitle.Validators;

public sealed class GetSubtitleByIdQueryValidator
    : AbstractValidator<GetSubtitleByIdQuery>
{
    public GetSubtitleByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Subtitle ID must be greater than zero.");
    }
}