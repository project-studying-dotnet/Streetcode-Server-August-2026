using FluentValidation;
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
            .WithMessage("News cannot be null.")
            .SetValidator(newsDtoValidator);
        RuleFor(command => command.news.Id)
            .GreaterThan(0)
            .WithMessage("News ID must be greater than 0.")
            .When(command => command.news is not null);
    }
}