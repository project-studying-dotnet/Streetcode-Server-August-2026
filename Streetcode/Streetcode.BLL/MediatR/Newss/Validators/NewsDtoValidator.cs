using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.News;
using NewsEntity = Streetcode.DAL.Entities.News.News;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class NewsDtoValidator
    : AbstractValidator<NewsDTO>
{
    public NewsDtoValidator()
    {
        RuleFor(news => news.Title)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.TitleMaxLength, "Title");
        RuleFor(news => news.Text)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required);
        RuleFor(news => news.URL)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
        RuleFor(news => news.CreationDate)
            .NotEmpty()
            .WithMessage(ErrorMessages.Field_Required);
    }
}