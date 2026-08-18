using FluentValidation;
using Streetcode.BLL.DTO.News;
using Streetcode.BLL.MediatR.Newss.Create;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class CreateNewsCommandValidator
    : AbstractValidator<CreateNewsCommand>
{
    public CreateNewsCommandValidator(IValidator<NewsDTO> newsDtoValidator)
    {
        RuleFor(command => command.newNews)
            .NotNull()
            .WithMessage("News cannot be null.")
            .SetValidator(newsDtoValidator);
    }
}