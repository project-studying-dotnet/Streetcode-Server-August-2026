using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Newss.GetNewsAndLinksByUrl;
using NewsEntity = Streetcode.DAL.Entities.News.News;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsAndLinksByUrlQueryValidator
    : AbstractValidator<GetNewsAndLinksByUrlQuery>
{
    public GetNewsAndLinksByUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
    }
}