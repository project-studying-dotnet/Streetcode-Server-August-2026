using FluentValidation;
using Streetcode.BLL.DTO.News;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class NewsDtoValidator
    : AbstractValidator<NewsDTO>
{
    public NewsDtoValidator()
    {
        RuleFor(news => news.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(150)
            .WithMessage("Title must not exceed 150 characters.");
        RuleFor(news => news.Text)
            .NotEmpty()
            .WithMessage("Text is required.");
        RuleFor(news => news.URL)
            .NotEmpty()
            .WithMessage("URL is required.")
            .MaximumLength(100)
            .WithMessage("URL must not exceed 100 characters.");
        RuleFor(news => news.CreationDate)
            .NotEmpty()
            .WithMessage("CreationDate is required.");
    }
}