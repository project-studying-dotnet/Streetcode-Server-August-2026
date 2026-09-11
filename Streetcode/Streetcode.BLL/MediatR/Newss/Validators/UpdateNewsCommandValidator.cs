using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.MediatR.Newss.Update;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class UpdateNewsCommandValidator
    : AbstractValidator<UpdateNewsCommand>
{
    public UpdateNewsCommandValidator(IValidator<NewsDTO> newsDtoValidator)
    {
        RuleFor(command => command.news)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(newsDtoValidator);
        RuleFor(command => command.news.Id)
            .MustBeValidId("News")
            .When(command => command.news is not null);
    }
}