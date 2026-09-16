using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Newss.GetByUrl;
using NewsEntity = Streetcode.DAL.Entities.News.News;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsByUrlQueryValidator
    : AbstractValidator<GetNewsByUrlQuery>
{
    public GetNewsByUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage("URL is required.")
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
    }
}