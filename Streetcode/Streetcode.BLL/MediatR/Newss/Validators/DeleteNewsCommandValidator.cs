using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Newss.Delete;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class DeleteNewsCommandValidator
    : AbstractValidator<DeleteNewsCommand>
{
    public DeleteNewsCommandValidator()
    {
        RuleFor(command => command.id)
            .MustBeValidId("News");
    }
}