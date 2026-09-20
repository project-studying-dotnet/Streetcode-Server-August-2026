using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Newss.GetNewsAndLinksByUrl;
using Streetcode.BLL.Resources;
using NewsEntity = Streetcode.DAL.Entities.News.News;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsAndLinksByUrlQueryValidator
    : AbstractValidator<GetNewsAndLinksByUrlQuery>
{
    public GetNewsAndLinksByUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithName("URL")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
    }
}