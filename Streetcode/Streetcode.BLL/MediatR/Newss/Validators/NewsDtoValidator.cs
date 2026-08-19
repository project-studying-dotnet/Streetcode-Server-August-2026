using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.News;
using NewsEntity = Streetcode.DAL.Entities.News.News;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class NewsDtoValidator
    : AbstractValidator<NewsDTO>
{
    public NewsDtoValidator()
    {
        RuleFor(news => news.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MustNotExceedLength(NewsEntity.TitleMaxLength, "Title");
        RuleFor(news => news.Text)
            .NotEmpty()
            .WithMessage("Text is required.");
        RuleFor(news => news.URL)
            .NotEmpty()
            .WithMessage("URL is required.")
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
        RuleFor(news => news.CreationDate)
            .NotEmpty()
            .WithMessage("CreationDate is required.");
    }
}