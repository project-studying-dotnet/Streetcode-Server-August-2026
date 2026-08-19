using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.MediatR.Newss.Update;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class UpdateNewsCommandValidator
    : AbstractValidator<UpdateNewsCommand>
{
    public UpdateNewsCommandValidator(IValidator<NewsDTO> newsDtoValidator)
    {
        RuleFor(command => command.news)
            .NotNull()
            .WithMessage("News is required.")
            .SetValidator(newsDtoValidator);
        RuleFor(command => command.news.Id)
            .MustBeValidId("News")
            .When(command => command.news is not null);
    }
}