using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.Resources;
using NewsEntity = Streetcode.DAL.Entities.News.News;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class NewsDtoValidator
    : AbstractValidator<NewsDTO>
{
    public NewsDtoValidator()
    {
        RuleFor(news => news.Title)
            .NotEmpty()
            .WithName("Title")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.TitleMaxLength, "Title");
        RuleFor(news => news.Text)
            .NotEmpty()
            .WithName("Text")
            .WithMessage(ErrorMessages.Field_Required);
        RuleFor(news => news.URL)
            .NotEmpty()
            .WithName("URL")
            .WithMessage(ErrorMessages.Field_Required)
            .MustNotExceedLength(NewsEntity.UrlMaxLength, "URL");
        RuleFor(news => news.CreationDate)
            .NotEmpty()
            .WithName("CreationDate")
            .WithMessage(ErrorMessages.Field_Required);
    }
}